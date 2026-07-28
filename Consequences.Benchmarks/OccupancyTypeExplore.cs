using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Numerics.Data;

namespace Consequences.Benchmarks;

// Why this test:
//   This explore decided how OccupancyType stores its damage functions. An
//   earlier revision of the model (commit 6d45874) carried both
//   representations side by side:
//
//     public required Func<float, float> StructureDamageFunction ...   // A
//     public required OrderedPairedData StructureDamageFunctionOrdinates ... // B
//
//   with Building.Compute dispatching through the Func wrapper and
//   Building.ComputeOrdinate calling OrderedPairedData.GetYFromX directly.
//   The two benchmarks (DepthVelocityHazard_Func vs the now commented-out
//   DepthVelocityHazard_Ordinate) A/B'd those paths over the same buildings.
//
// What happened / decision:
//   OccupancyType holds OrderedPairedData directly (see
//   Consequences/Occupancy/OccupancyType.cs). The Func<float,float> wrapper
//   added a delegate invocation plus a closure capture of the curve per
//   occupancy type without buying any abstraction the model needed — the
//   delegate body was just `d => (float)curve.GetYFromX(d)`. The losing
//   representation was removed from the model, which is why only one
//   benchmark still compiles; the original A/B numbers were not preserved.
//
//   Note the surviving benchmark's name is legacy: after the Func removal,
//   its body calls the same b.Compute(DepthVelocity) path as
//   DamageBenchmarks.DepthVelocityHazard_Struct. The 2026-07-13 run below
//   confirms they measure the same thing (within noise of each other), so
//   this class is retained as the record of the representation decision —
//   ongoing hazard-path measurement lives in DamageBenchmarks.
//
// Result (AMD EPYC 9654, Windows 11, .NET 9.0.17, 2026-07-13; ratio vs
// DamageBenchmarks.Depth_Primitive from the same joined run):
//
// | Method                                       | N       | Mean        | Ratio | Allocated |
// |----------------------------------------------|---------|------------:|------:|----------:|
// | DepthVelocityHazard_Func (this class)        |   1,000 |    19.47 us |  1.19 |         - |
// | DamageBenchmarks.DepthVelocityHazard_Struct  |   1,000 |    19.18 us |  1.17 |         - |
// | DepthVelocityHazard_Func (this class)        | 100,000 | 2,274.52 us |  1.08 |         - |
// | DamageBenchmarks.DepthVelocityHazard_Struct  | 100,000 | 2,263.00 us |  1.08 |         - |
[MemoryDiagnoser]
public class OccupancyTypeExplore
{
    [Params(1_000, 100_000)]
    public int StructureCount;

    private Building[] _buildings = Array.Empty<Building>();

    private float[] _depths = Array.Empty<float>();
    private float[] _velocities = Array.Empty<float>();

    private DepthHazard[] _depthHazards = Array.Empty<DepthHazard>();
    private DepthVelocity[] _depthVelocityHazards = Array.Empty<DepthVelocity>();

    [GlobalSetup]
    public void Setup()
    {
        var structureCurve = new OrderedPairedData(
            new double[] { 0.0, 10.0 }, new double[] { 0.0, 1.0 },
            strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Ascending);
        var contentCurve = new OrderedPairedData(
            new double[] { 0.0, 8.0 }, new double[] { 0.0, 1.0 },
            strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Ascending);

        // Preserved from the A/B: the dual-representation OccupancyType with
        // Func<float,float> wrappers alongside the raw ordinates. No longer
        // compiles — the Func properties were removed from the model.
        // var occupancy = new OccupancyType
        // {
        //     Name = "RES1",
        //     StructureDamageFunction = d => (float)structureCurve.GetYFromX(d),
        //     ContentDamageFunction = d => (float)contentCurve.GetYFromX(d),
        //     FoundationHeightOffset = 0f,
        //     StructureDamageFunctionOrdinates = structureCurve,
        //     ContentDamageFunctionOrdinates = contentCurve
        // };

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
        }
    }
    

    // Legacy name — originally the Func-wrapper side of the A/B; since the
    // Func removal this exercises the direct-ordinate Compute path.
    [Benchmark(Baseline = true)]
    public float DepthVelocityHazard_Func()
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

    // Preserved from the A/B: the direct-ordinate side, which called
    // Building.ComputeOrdinate (since folded into Building.Compute as the
    // only implementation).
    // [Benchmark]
    // public float DepthVelocityHazard_Ordinate()
    // {
    //     float total = 0;
    //     var buildings = _buildings;
    //     var hazards = _depthVelocityHazards;
    //     for (int i = 0; i < buildings.Length; i++)
    //     {
    //         ref var b = ref buildings[i];
    //         total += b.ComputeOrdinate(hazards[i]).Total;
    //     }
    //     return total;
    // }


}
