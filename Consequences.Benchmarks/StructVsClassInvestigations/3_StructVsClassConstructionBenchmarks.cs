using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks.StructVsClassInvestigations;

// Companion to 0_StructVsClassBenchmarks. Same six call shapes, same payload
// types, but the SampleStruct/SampleClass instances are constructed *inside*
// each benchmark's hot loop instead of once in [GlobalSetup]. #0 measured
// dispatch cost on a payload that already existed; this measures
// construction + dispatch together, which is what "build one per call" call
// sites (as opposed to "reuse one from an array") actually pay.
//
// [GlobalSetup] here only generates the raw A/B scalars — the struct/class
// instances themselves are built fresh on every loop iteration from those
// scalars, so the random source stays identical to #0 while the thing under
// test (payload construction) moves inside the timed region.
//
// Hypothesis:
//   - Struct paths: constructing a SampleStruct inline is a stack write, not
//     an allocation. Expect the same numbers as #0 — moving construction
//     into the loop shouldn't cost anything measurable.
//   - Class paths: constructing a SampleClass inline is a heap allocation
//     that #0 hid inside [GlobalSetup], outside the measured region. Expect
//     every class variant to now report ~N allocations of SampleClass and a
//     mean that includes allocation + GC cost, not just dispatch cost.
//
// Result (AMD EPYC 9654 2.40GHz, Windows 11, .NET 9.0.16, 2026-07-02):
//
// | Method             | N        | Mean             | Ratio | Allocated     |
// |--------------------|----------|-----------------:|------:|--------------:|
// | ViaConcreteStruct  |     1000 |        828.5 ns  |  1.00 |             - |
// | ViaGenericStruct   |     1000 |        826.1 ns  |  1.00 |             - |
// | ViaInterfaceStruct |     1000 |      2,499.5 ns  |  3.02 |      24,000 B |
// | ViaConcreteClass   |     1000 |      2,249.3 ns  |  2.72 |      24,000 B |
// | ViaGenericClass    |     1000 |      2,374.1 ns  |  2.87 |      24,000 B |
// | ViaInterfaceClass  |     1000 |      2,270.5 ns  |  2.74 |      24,000 B |
// | ViaConcreteStruct  |   100000 |     84,699.1 ns  |  1.00 |             - |
// | ViaGenericStruct   |   100000 |     84,193.4 ns  |  0.99 |             - |
// | ViaInterfaceStruct |   100000 |    248,394.5 ns  |  2.93 |   2,400,000 B |
// | ViaConcreteClass   |   100000 |    226,818.0 ns  |  2.68 |   2,400,000 B |
// | ViaGenericClass    |   100000 |    237,155.6 ns  |  2.80 |   2,400,000 B |
// | ViaInterfaceClass  |   100000 |    225,242.4 ns  |  2.66 |   2,400,000 B |
// | ViaConcreteStruct  | 10000000 |  8,526,485.0 ns  |  1.00 |             - |
// | ViaGenericStruct   | 10000000 |  8,545,829.5 ns  |  1.00 |             - |
// | ViaInterfaceStruct | 10000000 | 26,386,532.5 ns  |  3.09 | 240,000,000 B |
// | ViaConcreteClass   | 10000000 | 23,922,073.3 ns  |  2.81 | 240,000,000 B |
// | ViaGenericClass    | 10000000 | 55,034,167.4 ns  |  6.45 | 240,000,000 B |
// | ViaInterfaceClass  | 10000000 | 23,271,895.4 ns  |  2.73 | 240,000,000 B |
//
// Summary:
//
//   Struct paths came in exactly as hypothesized: ViaConcreteStruct and
//   ViaGenericStruct are identical at every N and allocate nothing.
//   Constructing a SampleStruct inside the loop is a stack write the JIT
//   folds straight into the call — moving it out of [GlobalSetup] cost
//   nothing. ViaInterfaceStruct also matches #0's shape: ~3x baseline with
//   one 24-byte box per call, unchanged by where construction happens
//   (it was always inside the timed region in #0 too, since #0 only
//   pre-built the *unboxed* struct array — the box still happens per-call
//   at the Interface() boundary in both files).
//
//   Class paths are where this file diverges from #0. #0 hid class
//   construction in [GlobalSetup], so ViaConcreteClass/ViaGenericClass/
//   ViaInterfaceClass looked nearly free (~1.1x baseline). Here, with
//   construction inside the timed loop, all three land at ~2.7-2.9x
//   baseline at N=1000/100000 — essentially matching ViaInterfaceStruct's
//   box cost. That makes sense: constructing a SampleClass and boxing a
//   SampleStruct are the same underlying operation (heap-allocate a
//   24-byte object with the same two-float layout), so their costs
//   converge once both are measured.
//
//   The one result that breaks the pattern: at N=10,000,000,
//   ViaGenericClass jumps to 6.45x (55.0 ms) while ViaConcreteClass and
//   ViaInterfaceClass stay at ~2.7-2.8x (~23-24 ms) — despite all three
//   reporting essentially the same Gen0 collection count
//   (28,666-28,688 per 1000 ops). The extra cost isn't allocation
//   pressure; something about the generic-over-reference-type dispatch
//   path specifically degrades at this scale. The measurement is tight
//   (StdDev ~0.4 ms on a 55 ms mean), so this isn't noise, but the
//   mechanism isn't identified — worth a follow-up.
//
// Major takeaway:
//
//   Where construction happens only matters for classes. For structs,
//   "construct inline" (this file) and "construct in [GlobalSetup]" (#0)
//   are indistinguishable — construction is free either way. For classes,
//   #0's numbers were measuring dispatch on payloads that already existed;
//   once construction is included, every class path costs roughly what
//   ViaInterfaceStruct's per-call box costs, because both are a 24-byte
//   heap allocation. The takeaway for real call sites: "hold a reused
//   class instance" and "hold a reused struct" are close in cost (per #0),
//   but "construct a class per call" and "construct a struct per call" are
//   not — the class pays a real, allocation-sized tax that the struct
//   never does.
[MemoryDiagnoser]
public class StructVsClassConstructionBenchmarks
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

    [Params(1_000, 100_000, 10_000_000)]
    public int N;

    private float[] _aValues = [];
    private float[] _bValues = [];

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _aValues = new float[N];
        _bValues = new float[N];
        for (int i = 0; i < N; i++)
        {
            _aValues[i] = (float)rng.NextDouble();
            _bValues[i] = (float)rng.NextDouble();
        }
    }

    // ---- struct paths (baseline) ----

    [Benchmark(Baseline = true)]
    public float ViaConcreteStruct()
    {
        float total = 0;
        var a = _aValues;
        var b = _bValues;
        for (int i = 0; i < a.Length; i++)
        {
            total += ConcreteStruct(new SampleStruct { A = a[i], B = b[i] });
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericStruct()
    {
        float total = 0;
        var a = _aValues;
        var b = _bValues;
        for (int i = 0; i < a.Length; i++)
        {
            total += Generic(new SampleStruct { A = a[i], B = b[i] });
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceStruct()
    {
        float total = 0;
        var a = _aValues;
        var b = _bValues;
        for (int i = 0; i < a.Length; i++)
        {
            total += Interface(new SampleStruct { A = a[i], B = b[i] });
        }
        return total;
    }

    // ---- class paths ----

    [Benchmark]
    public float ViaConcreteClass()
    {
        float total = 0;
        var a = _aValues;
        var b = _bValues;
        for (int i = 0; i < a.Length; i++)
        {
            total += ConcreteClass(new SampleClass { A = a[i], B = b[i] });
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericClass()
    {
        float total = 0;
        var a = _aValues;
        var b = _bValues;
        for (int i = 0; i < a.Length; i++)
        {
            total += Generic(new SampleClass { A = a[i], B = b[i] });
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceClass()
    {
        float total = 0;
        var a = _aValues;
        var b = _bValues;
        for (int i = 0; i < a.Length; i++)
        {
            total += Interface(new SampleClass { A = a[i], B = b[i] });
        }
        return total;
    }

    private static float ConcreteStruct(SampleStruct s) => s.A * 2f + s.B;
    private static float ConcreteClass(SampleClass s) => s.A * 2f + s.B;
    private static float Generic<T>(T s) where T : ISample => s.A * 2f + s.B;
    private static float Interface(ISample s) => s.A * 2f + s.B;
}
