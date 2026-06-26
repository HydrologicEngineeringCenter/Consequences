using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks.StructVsClassInvestigations;

// Why this test:
//   Mirror of InterfaceVsGenericBenchmarks (#0 in the Interface
//   investigations), but with a class variant of the same payload sitting
//   next to the struct. The interface tests left struct-vs-class as a
//   confounding variable — every "struct" result there was also a "value
//   type" result. Here the body is small enough that the cost of each call
//   shape is visible rather than buried under per-call work, so the struct/
//   class axis is what we're actually measuring.
//
//   Hypothesis:
//     - ViaConcreteStruct / ViaConcreteClass: both inline cleanly. Class
//       version pays one indirection per field read but no allocation; the
//       JIT may or may not match struct exactly depending on inlining.
//     - ViaGenericStruct: JIT specializes the body for the value type T —
//       no boxing, same as concrete.
//     - ViaGenericClass: reference-sharing kicks in (one shared compiled
//       body for all reference Ts), so the dispatch can't fully inline. No
//       boxing, but the call doesn't collapse the way the struct generic
//       does.
//     - ViaInterfaceStruct: per-call boxing + virtual interface dispatch —
//       the result that motivated the Interface investigation in the first
//       place. Expect ~4x slower with one allocation per call.
//     - ViaInterfaceClass: no boxing (already a heap object), virtual call.
//       Should be much closer to concrete than the struct interface path.
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method             | N        | Mean             | Ratio | Allocated     |
// |--------------------|----------|-----------------:|------:|--------------:|
// | ViaConcreteStruct  |     1000 |        542.0 ns  |  1.00 |             - |
// | ViaGenericStruct   |     1000 |        539.1 ns  |  0.99 |             - |
// | ViaInterfaceStruct |     1000 |      2,466.5 ns  |  4.55 |      24,000 B |
// | ViaConcreteClass   |     1000 |        585.7 ns  |  1.08 |             - |
// | ViaGenericClass    |     1000 |        610.3 ns  |  1.13 |             - |
// | ViaInterfaceClass  |     1000 |        611.5 ns  |  1.13 |             - |
// | ViaConcreteStruct  |   100000 |     59,491.5 ns  |  1.00 |             - |
// | ViaGenericStruct   |   100000 |     59,355.9 ns  |  1.00 |             - |
// | ViaInterfaceStruct |   100000 |    245,483.7 ns  |  4.13 |   2,400,000 B |
// | ViaConcreteClass   |   100000 |     64,178.3 ns  |  1.08 |             - |
// | ViaGenericClass    |   100000 |     66,128.5 ns  |  1.11 |             - |
// | ViaInterfaceClass  |   100000 |     66,527.4 ns  |  1.12 |             - |
// | ViaConcreteStruct  | 10000000 |  6,101,855.6 ns  |  1.00 |             - |
// | ViaGenericStruct   | 10000000 |  6,156,951.0 ns  |  1.01 |             - |
// | ViaInterfaceStruct | 10000000 | 27,242,683.5 ns  |  4.46 | 240,000,000 B |
// | ViaConcreteClass   | 10000000 |  6,778,718.0 ns  |  1.11 |             - |
// | ViaGenericClass    | 10000000 |  6,858,128.6 ns  |  1.12 |             - |
// | ViaInterfaceClass  | 10000000 |  6,851,233.1 ns  |  1.12 |             - |
//
// Reading the table:
//
//   Struct group (the original Interface investigation result, reproduced):
//   - Concrete and Generic land on the same number — JIT specializes
//     Generic<SampleStruct> for the value type and the body inlines.
//   - Interface is ~4x slower with one allocation per call. The struct gets
//     boxed for each ISample parameter and A/B turn into virtual interface
//     dispatches. This is the cost the Interface investigation was about.
//
//   Class group (the new data point):
//   - All three paths (Concrete, Generic, Interface) land within ~5% of
//     each other and within ~10–14% of the struct baseline. A class instance
//     is already a heap object, so the JIT can't avoid the indirection but
//     also doesn't have to box: the same reference is passed as a SampleClass,
//     a T, or an ISample.
//   - ViaGenericClass costs about the same as ViaInterfaceClass. Reference-
//     sharing means the JIT compiles one body for all reference Ts and uses
//     constrained-virtual dispatch — effectively the same code path as the
//     interface call. No per-struct specialization win for reference types.
//   - ViaInterfaceClass costs about the same as ViaConcreteClass. No boxing,
//     no per-call allocation, just a virtual call that the JIT can often
//     guarded-devirtualize.
//
// Headline:
//
//   The 4x cost we attributed to "the interface path" in the original
//   investigation was actually two costs glued together: (a) the per-call
//   box of a struct at an interface boundary, and (b) virtual dispatch.
//   Switching the payload to a class eliminates (a) entirely, leaving only
//   (b) — which the JIT can mostly absorb. So:
//
//     - If you'll hold something behind an interface, a class is ~3.5x
//       faster than a struct at that boundary.
//     - If you'll call concretely or through a generic constraint, a struct
//       is ~10% faster than a class.
//
//   Translation for Building: the production benchmark
//   (1_BuildingStructVsClassBenchmarks) shows none of these gaps survive
//   once the per-call body does real work — but the underlying mechanics
//   are what's visible here.
//
// L2 pressure (N = 10M):
//
//   SampleStruct[10M] = 80 MB dense, SampleClass[10M] = 80 MB of references
//   plus ~320 MB of heap objects — both far past M4's 16 MB perf-core L2.
//   The interesting question was whether the dense struct array beats the
//   sparse class array once both spill out of cache.
//
//   It does not, by much. Every ratio at N=10M is within 1% of the ratio at
//   N=100k:
//     - ViaConcreteStruct: 1.00x at 100k → 1.00x at 10M
//     - ViaConcreteClass:  1.08x at 100k → 1.11x at 10M
//     - ViaInterfaceStruct: 4.13x → 4.46x
//     - ViaInterfaceClass: 1.12x → 1.12x
//
//   Why the class array doesn't fall off a cliff: access is strictly
//   sequential, the hardware prefetcher catches the array-of-references
//   walk, and the SampleClass objects were allocated back-to-back in setup
//   so they're packed in the heap. With a one-multiply body, the prefetcher
//   has plenty of time to hide the indirection. To actually expose the
//   "dense struct vs. sparse class refs" gap you'd need either random
//   access, a heavier per-instance payload, or a per-call body small enough
//   that miss latency dominates — none of which apply here.
//
//   What is real: ViaInterfaceStruct at N=10M allocates 240 MB per
//   operation. That's still amortized over thousands of measurement
//   iterations and the GC keeps up, but it's a useful number to know — the
//   per-call box from the toy benchmark is not free, it's just normally
//   small enough that the GC absorbs it before it shows up as latency.
[MemoryDiagnoser]
public class StructVsClassBenchmarks
{
    public interface ISample
    {
        float A { get; }
        float B { get; }
    }

    public struct SampleStruct : ISample
    {
        public float A { get; set; }
        public float B { get; set; }
    }

    public class SampleClass : ISample
    {
        public float A { get; set; }
        public float B { get; set; }
    }

    // 10M added to push past L2: SampleStruct[10M] = 80 MB dense; SampleClass[10M]
    // is 80 MB of refs plus ~10M heap objects, so the class walk hits cold cache
    // lines on every load. M4 perf-core L2 is 16 MB / cluster.
    [Params(1_000, 100_000, 10_000_000)]
    public int N;

    private SampleStruct[] _structs = [];
    private SampleClass[] _classes = [];

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _structs = new SampleStruct[N];
        _classes = new SampleClass[N];
        for (int i = 0; i < N; i++)
        {
            float a = (float)rng.NextDouble();
            float b = (float)rng.NextDouble();
            _structs[i] = new SampleStruct { A = a, B = b };
            _classes[i] = new SampleClass { A = a, B = b };
        }
    }

    // ---- struct paths (baseline) ----

    [Benchmark(Baseline = true)]
    public float ViaConcreteStruct()
    {
        float total = 0;
        var samples = _structs;
        for (int i = 0; i < samples.Length; i++)
        {
            total += ConcreteStruct(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericStruct()
    {
        float total = 0;
        var samples = _structs;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Generic(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceStruct()
    {
        float total = 0;
        var samples = _structs;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Interface(samples[i]);
        }
        return total;
    }

    // ---- class paths ----

    [Benchmark]
    public float ViaConcreteClass()
    {
        float total = 0;
        var samples = _classes;
        for (int i = 0; i < samples.Length; i++)
        {
            total += ConcreteClass(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericClass()
    {
        float total = 0;
        var samples = _classes;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Generic(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceClass()
    {
        float total = 0;
        var samples = _classes;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Interface(samples[i]);
        }
        return total;
    }

    private static float ConcreteStruct(SampleStruct s) => s.A * 2f + s.B;
    private static float ConcreteClass(SampleClass s) => s.A * 2f + s.B;
    private static float Generic<T>(T s) where T : ISample => s.A * 2f + s.B;
    private static float Interface(ISample s) => s.A * 2f + s.B;
}
