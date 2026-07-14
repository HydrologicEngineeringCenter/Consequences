using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Numerics.Data;

namespace Consequences.Benchmarks;

// Why this test:
//   Building.Compute has overloads for hazard data of increasing richness:
//   a raw float depth, a DepthHazard struct, a DepthVelocity struct, and a
//   HydraulicTimeSeries class. The question is what the hazard
//   *representation* costs per call in the hot damage loop — is wrapping
//   the primitives in domain types free, and does reaching through a heap
//   object for the time-series case matter? HydraulicTimeSeries precomputes
//   its max statistics at construction, so this measures per-call
//   dereference/dispatch cost, not time-series processing.
//
//   The four benchmarks are the same summation loop over the same buildings;
//   only the hazard argument changes:
//     - Depth_Primitive (baseline)      — Building.ComputeMetal(float)
//     - DepthHazard_Struct              — Compute(DepthHazard)
//     - DepthVelocityHazard_Struct      — Compute(DepthVelocity)
//     - HydraulicTimeSeriesHazard_Class — Compute(HydraulicTimeSeries)
//
// Result (AMD EPYC 9654, Windows 11, .NET 9.0.17, 2026-07-13):
//
// | Method                          | N       | Mean        | Ratio | Allocated |
// |---------------------------------|---------|------------:|------:|----------:|
// | Depth_Primitive                 |   1,000 |    16.37 us |  1.00 |         - |
// | DepthHazard_Struct              |   1,000 |    16.21 us |  0.99 |         - |
// | DepthVelocityHazard_Struct      |   1,000 |    19.18 us |  1.17 |         - |
// | HydraulicTimeSeriesHazard_Class |   1,000 |    20.39 us |  1.25 |         - |
// | Depth_Primitive                 | 100,000 | 2,102.18 us |  1.00 |         - |
// | DepthHazard_Struct              | 100,000 | 2,124.16 us |  1.01 |         - |
// | DepthVelocityHazard_Struct      | 100,000 | 2,263.00 us |  1.08 |         - |
// | HydraulicTimeSeriesHazard_Class | 100,000 | 2,403.65 us |  1.14 |         - |
//
// What happened:
//   - Wrapping a float in a DepthHazard struct is free (0.99–1.01x): the
//     struct is a single field, passed by value, and the JIT sees through it.
//   - The DepthVelocity path costs ~8% at N=100k, but that is not the
//     representation — the depth+velocity overload does more work per call
//     (effectiveDepth <= 0 early-out branch and the SampledStabilityCriteria
//     null check), roughly 1.5 ns/building of extra logic.
//   - The time-series path adds another ~6% on top of DepthVelocity: the
//     hazard is a heap object, so MaxDepth/MaxVelocity are property reads
//     through a class reference rather than struct fields already in
//     registers. Still ~3 ns/building over the primitive baseline.
//   - Nothing allocates on any path.
//
// Decision:
//   The hazard type hierarchy stays as designed — structs for scalar hazards,
//   a class for the time series. At ~21 ns/building for the whole damage
//   compute, the representation penalty is small enough that flattening
//   HydraulicTimeSeries into a struct for the damage loop is not warranted;
//   callers that only need max stats can always extract a DepthVelocity
//   themselves if a few percent matters at scale.
[MemoryDiagnoser]
public class DamageBenchmarks
{
    [Params(1_000, 100_000)]
    public int StructureCount;

    private Building[] _buildings = Array.Empty<Building>();

    private float[] _depths = Array.Empty<float>();
    private float[] _velocities = Array.Empty<float>();

    private DepthHazard[] _depthHazards = Array.Empty<DepthHazard>();
    private DepthVelocity[] _depthVelocityHazards = Array.Empty<DepthVelocity>();
    private HydraulicTimeSeries[] _timeSeriesHazards = Array.Empty<HydraulicTimeSeries>();

    [GlobalSetup]
    public void Setup()
    {
        var structureCurve = new OrderedPairedData(
            new double[] { 0.0, 10.0 }, new double[] { 0.0, 1.0 },
            strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Ascending);
        var contentCurve = new OrderedPairedData(
            new double[] { 0.0, 8.0 }, new double[] { 0.0, 1.0 },
            strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Ascending);

        var occupancy = new OccupancyType
        {
            Name = "RES1",
            FoundationHeightOffset = 0f,
            StructureDamageFunction = structureCurve,
            ContentDamageFunction = contentCurve
        };

        var rng = new Random(42);
        _buildings = new Building[StructureCount];
        _depths = new float[StructureCount];
        _velocities = new float[StructureCount];
        _depthHazards = new DepthHazard[StructureCount];
        _depthVelocityHazards = new DepthVelocity[StructureCount];
        _timeSeriesHazards = new HydraulicTimeSeries[StructureCount];

        for (int i = 0; i < StructureCount; i++)
        {
            _buildings[i] = new Building
            {
                OccupancyType = occupancy,
                Value = (float)(100_000 + rng.NextDouble() * 200_000),
                ContentValue = (float)(50_000 + rng.NextDouble() * 100_000),
                FoundationHeight = (float)(rng.NextDouble() * 3.0)
            };

            float depth = (float)(rng.NextDouble() * 12.0);
            float velocity = (float)(rng.NextDouble() * 5.0);
            _depths[i] = depth;
            _velocities[i] = velocity;

            _depthHazards[i] = new DepthHazard(depth);
            _depthVelocityHazards[i] = new DepthVelocity(depth, velocity);
            _timeSeriesHazards[i] = BuildTimeSeries(depth, velocity);
        }
    }

    private static HydraulicTimeSeries BuildTimeSeries(float peakDepth, float peakVelocity)
    {
        // Triangular rise-and-fall whose peak matches the scalar base data,
        // so all hazard types resolve to the same Depth/Velocity values.
        float[] times = { 0f, 30f, 60f, 90f, 120f };
        float[] depths = { 0f, peakDepth * 0.5f, peakDepth, peakDepth * 0.5f, 0f };
        float[] velocities = { 0f, peakVelocity * 0.5f, peakVelocity, peakVelocity * 0.5f, 0f };
        return new HydraulicTimeSeries(times, depths, velocities, pointReductionTolerance: 0.001f);
    }

    // Lower bound: raw float depth into the bare-metal compute path.
    [Benchmark(Baseline = true)]
    public float Depth_Primitive()
    {
        float total = 0;
        var buildings = _buildings;
        var depths = _depths;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += Building.ComputeMetal(depths[i],b);
        }
        return total;
    }

    // Same scalar depth, wrapped in the single-field DepthHazard struct.
    [Benchmark]
    public float DepthHazard_Struct()
    {
        float total = 0;
        var buildings = _buildings;
        var hazards = _depthHazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.Compute(hazards[i]).Total;
        }
        return total;
    }

    // Depth+velocity struct; the overload adds an early-out branch and a
    // stability-criteria check, so its delta over baseline is extra work,
    // not representation cost.
    [Benchmark]
    public float DepthVelocityHazard_Struct()
    {
        float total = 0;
        var buildings = _buildings;
        var hazards = _depthVelocityHazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.Compute(hazards[i]).Total;
        }
        return total;
    }

    // Full time-series hazard held as a class; max stats were precomputed at
    // construction, so this isolates the cost of reading them through a heap
    // reference inside the loop.
    [Benchmark]
    public float HydraulicTimeSeriesHazard_Class()
    {
        float total = 0;
        var buildings = _buildings;
        var hazards = _timeSeriesHazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.Compute(hazards[i]).Total;
        }
        return total;
    }
}
