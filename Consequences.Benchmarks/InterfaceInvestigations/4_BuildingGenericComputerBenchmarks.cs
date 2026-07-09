using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;
using Numerics.Data;

namespace Consequences.Benchmarks.InterfaceInvestigations;

// Production-types analogue of GenericComputerInterfaceBenchmarks. Same three
// call shapes (Concrete / Generic / Interface) but using the real domain
// types: Building as the receptor, DepthHazard as the input, DamageResult as
// the output. Building.Compute does actual work (foundation offset, value
// scaling, depth-damage curve interpolation via OrderedPairedData), so this
// answers whether the zero-allocation result from the toy benchmark survives
// when the per-call body is no longer a single multiply.
[MemoryDiagnoser]
public class BuildingGenericComputerBenchmarks
{
    [Params(1_000, 100_000)]
    public int N;

    private DepthHazard[] _hazards = [];
    private Building _building;

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
            ContentDamageFunction = contentCurve,
        };

        _building = new Building
        {
            OccupancyType = occupancy,
            Value = 200_000f,
            ContentValue = 100_000f,
            FoundationHeight = 1.5f
        };

        var rng = new Random(42);
        _hazards = new DepthHazard[N];
        for (int i = 0; i < N; i++)
        {
            _hazards[i] = new DepthHazard((float)(rng.NextDouble() * 12.0));
        }
    }

    [Benchmark(Baseline = true)]
    public float ViaConcrete()
    {
        float total = 0;
        var hazards = _hazards;
        var building = _building;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += building.Compute(hazards[i]).Total;
        }
        return total;
    }

    // Generic constraint all the way through: TReceptor is a value type,
    // THazard and TResult are concrete value types. The JIT should specialize
    // the whole chain, leaving nothing to box.
    [Benchmark]
    public float ViaGeneric()
    {
        float total = 0;
        var hazards = _hazards;
        var building = _building;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += Generic<Building, DepthHazard, DamageResult>(building, hazards[i]).Total;
        }
        return total;
    }

    // Holding the receptor as IConsequenceReceptor<DepthHazard, DamageResult>.
    // The Building struct boxes once at assignment, so per-call we pay
    // interface dispatch on Compute — but THazard/TResult are concrete value
    // types in the signature, so the hazard and result themselves do not box.
    [Benchmark]
    public float ViaInterface()
    {
        float total = 0;
        var hazards = _hazards;
        IConsequenceReceptor<DepthHazard, DamageResult> receptor = _building;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += receptor.Compute(hazards[i]).Total;
        }
        return total;
    }

    private static TResult Generic<TReceptor, THazard, TResult>(TReceptor receptor, THazard hazard)
        where TReceptor : IConsequenceReceptor<THazard, TResult>
        where THazard : struct, IHazard
        where TResult : struct, IConsequenceResult
        => receptor.Compute(hazard);
}
