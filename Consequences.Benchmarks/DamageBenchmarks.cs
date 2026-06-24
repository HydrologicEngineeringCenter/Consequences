using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;

namespace Consequences.Benchmarks;

[MemoryDiagnoser]
public class DamageBenchmarks
{
    [Params(1_000, 100_000)]
    public int StructureCount;

    private Building[] _buildings = Array.Empty<Building>();
    private Hazard[] _hazards = Array.Empty<Hazard>();

    [GlobalSetup]
    public void Setup()
    {
        var occupancy = new OccupancyType
        {
            Name = "RES1",
            StructureDamageFunction = static d => Math.Clamp(d / 10.0, 0.0, 1.0),
            ContentDamageFunction = static d => Math.Clamp(d / 8.0, 0.0, 1.0),
            OtherDamageFunction = static _ => 0.0,
            VehicleDamageFunction = static _ => 0.0,
            FoundationHeightOffset = 0.0,
        };

        var rng = new Random(42);
        _buildings = new Building[StructureCount];
        _hazards = new Hazard[StructureCount];
        for (int i = 0; i < StructureCount; i++)
        {
            _buildings[i] = new Building
            {
                OccupancyType = occupancy,
                Value = 100_000 + rng.NextDouble() * 200_000,
                ContentValue = 50_000 + rng.NextDouble() * 100_000,
                FoundationHeight = rng.NextDouble() * 3.0,
                NumStories = 1,
                FloorHeight = 9.0,
                AbleBodiedPeople = 2,
                LimitedMobilityPeople = 0,
            };
            _hazards[i] = new Hazard(
                depth: rng.NextDouble() * 12.0,
                velocity: rng.NextDouble() * 5.0,
                duration: 1.0);
        }
    }

    [Benchmark(Baseline = true)]
    public double Alt2_StructReturn()
    {
        double total = 0;
        var buildings = _buildings;
        var hazards = _hazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.ComputeComponents(hazards[i].Depth, hazards[i].Velocity).Total;
        }
        return total;
    }

    [Benchmark]
    public double Alt3_OutParams()
    {
        double total = 0;
        var buildings = _buildings;
        var hazards = _hazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.Compute(hazards[i].Depth, hazards[i].Velocity, out _, out _);
        }
        return total;
    }

    [Benchmark]
    public double Alt4_OutStruct()
    {
        double total = 0;
        var buildings = _buildings;
        var hazards = _hazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            b.Compute(hazards[i].Depth, hazards[i].Velocity, out DamageResult result);
            total += result.Total;
        }
        return total;
    }

    [Benchmark]
    public double Alt5_TotalOnly()
    {
        double total = 0;
        var buildings = _buildings;
        var hazards = _hazards;
        for (int i = 0; i < buildings.Length; i++)
        {
            ref var b = ref buildings[i];
            total += b.Compute(hazards[i].Depth, hazards[i].Velocity);
        }
        return total;
    }

    [Benchmark]
    public double Alt6_BatchedAPI()
    {
        double total = 0;
        var results = BuildingBatch.ComputeBatch(_buildings, _hazards);
        for (int i = 0; i < results.Length; i++)
            total += results[i].Total;
        return total;
    }

    [Benchmark]
    public double Alt7_BatchedTotalOnly()
    {
        return BuildingBatch.ComputeBatchTotal(_buildings, _hazards);
    }
}
