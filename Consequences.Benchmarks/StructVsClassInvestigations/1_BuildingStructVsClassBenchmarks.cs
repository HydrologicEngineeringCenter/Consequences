using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;
using Numerics.Data;

namespace Consequences.Benchmarks.StructVsClassInvestigations;

// Why this test:
//   The Interface investigations established that with the right shape
//   (generic, value-type all the way through) we can hold a Building behind
//   IConsequenceReceptor<DepthHazard, DamageResult> with no boxing and no
//   per-call cost. That whole result rests on Building being a *struct*.
//   The question here is: how much of the cost we measured was actually
//   "struct vs. class" rather than "concrete vs. interface"? If we keep
//   Building's API identical but change `struct` → `class`, what happens to
//   each of the three call shapes?
//
//   The hypothesis going in:
//     - ViaConcrete: should be very close. Both shapes pass an instance by
//       value-or-reference and call a method. The class version pays one
//       extra indirection per field read but avoids the cost of copying the
//       whole Building each call.
//     - ViaGeneric: should also be close, but for a different reason. The
//       JIT shares one compiled body across all reference-type Ts (reference
//       sharing), so we lose the per-struct specialization that made the
//       struct generic path identical to ViaConcrete in the Interface tests.
//       We still expect no boxing, just slightly less aggressive inlining.
//     - ViaInterface: this is where the cost should appear. A class instance
//       *is* a heap object, so holding it as IConsequenceReceptor<...> does
//       not box and does not allocate per-call — but the dispatch on Compute
//       is still virtual. Compared to the struct's ViaInterface (which boxes
//       the struct once at assignment, then dispatches virtually), the class
//       version should allocate nothing and be roughly the same per-call.
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method             | N        | Mean              | Ratio | Allocated |
// |--------------------|----------|------------------:|------:|----------:|
// | ViaConcreteStruct  |     1000 |       9.112 us    |  1.00 |         - |
// | ViaGenericStruct   |     1000 |       9.131 us    |  1.00 |         - |
// | ViaInterfaceStruct |     1000 |       9.250 us    |  1.02 |         - |
// | ViaConcreteClass   |     1000 |       9.273 us    |  1.02 |         - |
// | ViaGenericClass    |     1000 |       9.254 us    |  1.02 |         - |
// | ViaInterfaceClass  |     1000 |       9.514 us    |  1.04 |         - |
// | ViaConcreteStruct  |   100000 |   1,183.698 us    |  1.00 |         - |
// | ViaGenericStruct   |   100000 |   1,157.401 us    |  0.98 |         - |
// | ViaInterfaceStruct |   100000 |   1,183.270 us    |  1.00 |         - |
// | ViaConcreteClass   |   100000 |   1,172.754 us    |  0.99 |         - |
// | ViaGenericClass    |   100000 |   1,164.590 us    |  0.98 |         - |
// | ViaInterfaceClass  |   100000 |   1,232.338 us    |  1.04 |         - |
// | ViaConcreteStruct  | 10000000 | 121,162.237 us    |  1.00 |         - |
// | ViaGenericStruct   | 10000000 | 120,475.492 us    |  0.99 |         - |
// | ViaInterfaceStruct | 10000000 | 122,888.189 us    |  1.01 |         - |
// | ViaConcreteClass   | 10000000 | 123,150.598 us    |  1.02 |         - |
// | ViaGenericClass    | 10000000 | 123,280.533 us    |  1.02 |         - |
// | ViaInterfaceClass  | 10000000 | 128,471.319 us    |  1.06 |         - |
//
// All six paths land within ~5% of each other and none of them allocate.
// On the production workload the per-call body is dominated by two
// OrderedPairedData.GetYFromX curve interpolations — that's the heaviest
// thing happening per hazard, and it swamps any difference in how Building
// itself is passed around. Struct vs. class barely registers here.
//
// What's worth noting from the no-allocation column:
//   - ViaInterfaceStruct holds Building boxed once at assignment (the local
//     `receptor`). That box exists, but it's reused for every call, so the
//     per-op allocation is zero. The struct does *not* re-box per call — that
//     was the lesson from GenericComputerInterfaceBenchmarks (#3 in the
//     Interface investigations): with THazard/TResult constrained to struct
//     value types in the interface signature, no boxing happens at the
//     parameter boundary.
//   - ViaInterfaceClass never had a box to begin with. A class instance *is*
//     a heap object, so holding it through the interface is just a reference
//     copy and a virtual call. The 1.05x cost is the virtual dispatch the
//     class can't devirtualize as confidently as the boxed-struct case
//     (where there's exactly one box and tiered PGO can lock onto it).
//
// Bottom line for this codebase: if Building's per-call body stays heavy
// (curve interpolation, stability checks), switching it between struct and
// class is a wash for performance. The struct choice is justified by other
// concerns (value semantics, predictable layout, no GC pressure for arrays
// of Building) — not by hot-path speed on this workload.
//
// L2 pressure (N = 10M):
//
//   DepthHazard is 4 bytes, so _hazards[10M] = 40 MB — well past M4's 16 MB
//   perf-core L2. Per-call time scales linearly from N=100k (1,183 us) to
//   N=10M (121,162 us): exactly 100x for a 100x increase, no cache cliff.
//   All six ratios stay within 1–6% of the baseline, the same envelope as
//   at smaller N. The hazard stream is read strictly sequentially, so the
//   hardware prefetcher walks ahead of the load and L2/L3 misses are
//   serviced before they can stall the pipeline.
//
//   Caveat (important): this benchmark uses *one* Building / BuildingClass
//   instance per run. The receptor stays hot in L1 across the whole loop,
//   so this is not a test of "dense Building[] array vs. sparse
//   BuildingClass[] array of references." That's the experiment that would
//   actually punish a class layout — a separate follow-up.
//
// See 0_StructVsClassBenchmarks for the toy version where the per-call body
// is small enough that struct vs. class differences actually show up.
[MemoryDiagnoser]
public class BuildingStructVsClassBenchmarks
{
    // 10M added to push past L2 on the hazard stream. DepthHazard is 4 bytes,
    // so _hazards[10M] = 40 MB, well over M4's 16 MB perf-core L2. The Building
    // / BuildingClass instance itself is reused per call, so this stresses the
    // hazard read pattern more than the receptor layout — but it answers
    // whether the no-allocation, within-noise result at N=100k survives once
    // the input stream is bigger than cache.
    [Params(1_000, 100_000, 10_000_000)]
    public int N;

    private DepthHazard[] _hazards = [];
    private Building _buildingStruct;
    private BuildingClass _buildingClass = null!;

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

        _buildingStruct = new Building
        {
            OccupancyType = occupancy,
            Value = 200_000f,
            ContentValue = 100_000f,
            FoundationHeight = 1.5f
        };

        _buildingClass = new BuildingClass
        {
            OccupancyType = occupancy,
            Value = 200_000f,
            ContentValue = 100_000f,
            FoundationHeight = 1.5f,
            NumStories = 1,
        };

        var rng = new Random(42);
        _hazards = new DepthHazard[N];
        for (int i = 0; i < N; i++)
        {
            _hazards[i] = new DepthHazard((float)(rng.NextDouble() * 12.0));
        }
    }

    // ---- struct paths (baseline group) ----

    [Benchmark(Baseline = true)]
    public float ViaConcreteStruct()
    {
        float total = 0;
        var hazards = _hazards;
        var building = _buildingStruct;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += building.Compute(hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericStruct()
    {
        float total = 0;
        var hazards = _hazards;
        var building = _buildingStruct;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += Generic<Building, DepthHazard, DamageResult>(building, hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceStruct()
    {
        float total = 0;
        var hazards = _hazards;
        IConsequenceReceptor<DepthHazard, DamageResult> receptor = _buildingStruct;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += receptor.Compute(hazards[i]).Total;
        }
        return total;
    }

    // ---- class paths ----

    [Benchmark]
    public float ViaConcreteClass()
    {
        float total = 0;
        var hazards = _hazards;
        var building = _buildingClass;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += building.Compute(hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericClass()
    {
        float total = 0;
        var hazards = _hazards;
        var building = _buildingClass;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += Generic<BuildingClass, DepthHazard, DamageResult>(building, hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceClass()
    {
        float total = 0;
        var hazards = _hazards;
        IConsequenceReceptor<DepthHazard, DamageResult> receptor = _buildingClass;
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
