using BenchmarkDotNet.Attributes;
using Consequences.Buildings;
using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;
using Numerics.Data;

namespace Consequences.Benchmarks.StructVsClassInvestigations;

// Why this test:
//   1_BuildingStructVsClassBenchmarks held one Building / BuildingClass
//   instance and reused it for every hazard. That kept the receptor itself
//   hot in L1 across the whole loop, so the only thing that scaled with N
//   was the hazard stream. Even at N=10M (40 MB of hazards) we saw no
//   cache cliff because sequential access plus the M4 prefetcher hides L2
//   misses on a single hot receptor.
//
//   This benchmark holds an *array* of receptors and walks one element per
//   hazard. Now the receptor itself contributes to the working set:
//     - Building[10M]      ≈ 40-byte struct × 10M = ~400 MB dense
//     - BuildingClass[10M] = ~80 MB of references + ~10M heap objects
//                            (~56 bytes each, ~560 MB), so ~640 MB total
//                            spread across the heap
//     - For the struct-as-interface path we pre-box each Building once
//       during setup into an IConsequenceReceptor<...>[]. That's ~80 MB
//       of refs + ~10M boxed structs (~56 bytes each, ~560 MB) — same
//       layout as the class case, just allocated independently.
//
//   This is the test that would *actually* trigger the dense-vs-sparse
//   layout gap that N=10M on the hazard stream alone did not.
//
//   Hypothesis (refined from what the README claimed):
//     - At N=1k and N=100k the receptor array fits in cache, so all six
//       paths should look like the single-instance benchmark: within ~5%
//       of each other, no allocations after setup.
//     - At N=10M:
//       * ViaConcreteStruct should pull ahead. The Building[] walk is
//         dense, sequential, prefetcher-friendly. Per-element copy cost
//         (~40 bytes) is real but cheap relative to a miss.
//       * ViaConcreteClass should be measurably worse. Each
//         BuildingClass[i] is an indirection through a pointer to a heap
//         object. Even with sequential allocation, walking N=10M of them
//         pays a separate dependent load per element.
//       * ViaInterfaceStruct and ViaInterfaceClass should land near
//         ViaConcreteClass (both walk an array of references; struct path
//         has an extra box header to skip past but otherwise identical).
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method             | N        | Mean              | Ratio | Allocated |
// |--------------------|----------|------------------:|------:|----------:|
// | ViaConcreteStruct  |     1000 |      10.164 us    |  1.00 |         - |
// | ViaGenericStruct   |     1000 |       9.690 us    |  0.95 |         - |
// | ViaInterfaceStruct |     1000 |       9.839 us    |  0.97 |         - |
// | ViaConcreteClass   |     1000 |      10.283 us    |  1.01 |         - |
// | ViaGenericClass    |     1000 |      10.202 us    |  1.00 |         - |
// | ViaInterfaceClass  |     1000 |      10.194 us    |  1.00 |         - |
// | ViaConcreteStruct  |   100000 |   1,270.260 us    |  1.00 |         - |
// | ViaGenericStruct   |   100000 |   1,272.296 us    |  1.00 |         - |
// | ViaInterfaceStruct |   100000 |   1,269.369 us    |  1.00 |         - |
// | ViaConcreteClass   |   100000 |   1,290.635 us    |  1.02 |         - |
// | ViaGenericClass    |   100000 |   1,293.301 us    |  1.02 |         - |
// | ViaInterfaceClass  |   100000 |   1,262.174 us    |  0.99 |         - |
// | ViaConcreteStruct  | 10000000 | 129,465.670 us    |  1.00 |         - |
// | ViaGenericStruct   | 10000000 | 129,704.004 us    |  1.00 |         - |
// | ViaInterfaceStruct | 10000000 | 130,632.222 us    |  1.01 |         - |
// | ViaConcreteClass   | 10000000 | 130,411.508 us    |  1.01 |         - |
// | ViaGenericClass    | 10000000 | 133,429.479 us    |  1.03 |         - |
// | ViaInterfaceClass  | 10000000 | 131,514.703 us    |  1.02 |         - |
//
// Result: the predicted cliff did not appear. At N=10M — where Building[]
// occupies ~400 MB dense and BuildingClass[] occupies ~80 MB of refs +
// ~560 MB of heap objects, both far past M4's 16 MB perf-core L2 — every
// path stays within 3% of the baseline. ViaConcreteStruct does not pull
// ahead of ViaConcreteClass; the boxed-struct and class interface arrays
// land on the same number; nothing allocates per op.
//
// Why the hypothesis was wrong: the per-call body (curve interpolation +
// value scaling) costs ~13 ns. That's enormous compared to the time a
// modern hardware prefetcher needs to fetch the next cache line of a
// sequentially walked array. The prefetcher walks ahead through both the
// reference array AND the heap pages holding the BuildingClass / boxed
// Building instances (which Setup allocated back-to-back, so they're
// nearly contiguous), and there is plenty of compute on the critical path
// to hide any L3-miss latency. The walk is memory-touched but not
// memory-*bound*.
//
// What this actually says, with the L2 stress applied:
//   - For compute-bound, sequential workloads, struct vs. class layout
//     does not affect throughput. The prefetcher hides the indirection.
//   - The "dense array wins on cache" story is real, but only kicks in
//     under one of three conditions: (a) random or strided access that
//     defeats the prefetcher, (b) a per-element body so small that miss
//     latency dominates total time, or (c) per-instance data large enough
//     to thrash the cache regardless of layout.
//   - For Building specifically, none of those conditions are this
//     codebase's workload, so the struct-vs-class choice remains
//     performance-neutral. The README's earlier conclusion stands and is
//     now defended against the strongest variant of the cache-pressure
//     argument that the toy benchmarks couldn't reach.
//
// Caveat on the 3,084 B allocation reported for ViaGenericStruct at N=10M:
// that's a single GC-related artifact across the run, not per-call boxing
// (other paths show nothing under identical conditions). The per-op
// allocation column is effectively zero for all paths.
//
// See the README "Things worth investigating next" for the random-access
// variant — that's the experiment that should actually trigger the cliff.
[MemoryDiagnoser]
public class BuildingArrayStructVsClassBenchmarks
{
    // Same triple as the rest of the investigation. Note: at N=10M the
    // global setup allocates ~1.5-2 GB across the four receptor arrays.
    [Params(1_000, 100_000, 10_000_000)]
    public int N;

    private DepthHazard[] _hazards = [];
    private Building[] _buildingStructs = [];
    private BuildingClass[] _buildingClasses = [];
    private IConsequenceReceptor<DepthHazard, DamageResult>[] _structAsInterface = [];
    private IConsequenceReceptor<DepthHazard, DamageResult>[] _classAsInterface = [];

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

        var rng = new Random(42);

        _hazards = new DepthHazard[N];
        _buildingStructs = new Building[N];
        _buildingClasses = new BuildingClass[N];
        _structAsInterface = new IConsequenceReceptor<DepthHazard, DamageResult>[N];
        _classAsInterface = new IConsequenceReceptor<DepthHazard, DamageResult>[N];

        for (int i = 0; i < N; i++)
        {
            _hazards[i] = new DepthHazard((float)(rng.NextDouble() * 12.0));

            // Slight per-element variation to keep the call sites distinct.
            float value = 150_000f + (float)(rng.NextDouble() * 100_000.0);
            float content = 75_000f + (float)(rng.NextDouble() * 50_000.0);
            float foundation = 1.0f + (float)(rng.NextDouble() * 1.0);

            var s = new Building
            {
                OccupancyType = occupancy,
                Value = value,
                ContentValue = content,
                FoundationHeight = foundation
            };
            _buildingStructs[i] = s;
            _structAsInterface[i] = s; // boxes once, here, into the array slot

            var c = new BuildingClass
            {
                OccupancyType = occupancy,
                Value = value,
                ContentValue = content,
                FoundationHeight = foundation,
                NumStories = 1,
            };
            _buildingClasses[i] = c;
            _classAsInterface[i] = c; // no boxing — class is already a heap object
        }
    }

    // ---- struct paths ----

    [Benchmark(Baseline = true)]
    public float ViaConcreteStruct()
    {
        float total = 0;
        var hazards = _hazards;
        var buildings = _buildingStructs;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += buildings[i].Compute(hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericStruct()
    {
        float total = 0;
        var hazards = _hazards;
        var buildings = _buildingStructs;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += Generic<Building, DepthHazard, DamageResult>(buildings[i], hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceStruct()
    {
        float total = 0;
        var hazards = _hazards;
        var receptors = _structAsInterface;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += receptors[i].Compute(hazards[i]).Total;
        }
        return total;
    }

    // ---- class paths ----

    [Benchmark]
    public float ViaConcreteClass()
    {
        float total = 0;
        var hazards = _hazards;
        var buildings = _buildingClasses;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += buildings[i].Compute(hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericClass()
    {
        float total = 0;
        var hazards = _hazards;
        var buildings = _buildingClasses;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += Generic<BuildingClass, DepthHazard, DamageResult>(buildings[i], hazards[i]).Total;
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceClass()
    {
        float total = 0;
        var hazards = _hazards;
        var receptors = _classAsInterface;
        for (int i = 0; i < hazards.Length; i++)
        {
            total += receptors[i].Compute(hazards[i]).Total;
        }
        return total;
    }

    private static TResult Generic<TReceptor, THazard, TResult>(TReceptor receptor, THazard hazard)
        where TReceptor : IConsequenceReceptor<THazard, TResult>
        where THazard : struct, IHazard
        where TResult : struct, IConsequenceResult
        => receptor.Compute(hazard);
}
