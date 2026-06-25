using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Numerics.Data;

namespace Consequences.Benchmarks;

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

        var occupancy = new OccupancyType
        {
            Name = "RES1",
            StructureDamageFunction = d => (float)structureCurve.GetYFromX(d),
            ContentDamageFunction = d => (float)contentCurve.GetYFromX(d),
            FoundationHeightOffset = 0f,
            StructureDamageFunctionOrdinates = structureCurve,
            ContentDamageFunctionOrdinates = contentCurve
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
                FoundationHeight = (float)(rng.NextDouble() * 3.0),
                NumStories = 1,
                FloorHeight = 9f,
                AbleBodiedPeople = 2,
                LimitedMobilityPeople = 0,
            };

            float depth = (float)(rng.NextDouble() * 12.0);
            float velocity = (float)(rng.NextDouble() * 5.0);
            _depths[i] = depth;
            _velocities[i] = velocity;

            _depthHazards[i] = new DepthHazard(depth);
            _depthVelocityHazards[i] = new DepthVelocity(depth, velocity);
        }
    }
    

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

    [Benchmark]
    public float DepthVelocityHazard_Ordinate()
    {
        float total = 0;
        var buildings = _buildings;
        var hazards = _depthVelocityHazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.ComputeOrdinate(hazards[i]).Total;
        }
        return total;
    }


}
