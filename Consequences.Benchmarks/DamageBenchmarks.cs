using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Numerics.Data;

namespace Consequences.Benchmarks;

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
