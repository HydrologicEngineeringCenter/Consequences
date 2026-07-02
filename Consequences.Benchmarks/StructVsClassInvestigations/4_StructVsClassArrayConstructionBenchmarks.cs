using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks.StructVsClassInvestigations;

// Second companion to 0_StructVsClassBenchmarks, sitting between #0 and #3
// on the "when does construction happen" axis:
//
//   - #0: array of SampleStruct/SampleClass built once in [GlobalSetup],
//     entirely outside the timed region. Pure dispatch cost.
//   - #3: each instance is built and immediately consumed inline, one at a
//     time, inside the timed loop. Construction and dispatch interleaved
//     per element.
//   - #4 (this file): each benchmark builds its own SampleStruct[]/
//     SampleClass[] array *inside* the timed method, as a distinct
//     preprocessing pass, then dispatches over the fully-materialized array
//     in a second pass. Construction and dispatch are both timed, but as
//     two separate phases rather than interleaved.
//
// This is the shape of a call site that builds a batch of DTOs/payloads and
// then processes them, as opposed to #3's "build one, use one, repeat."
// [GlobalSetup] still only generates the raw A/B scalars, same as #3, so the
// random source is identical across all four files.
//
// Hypothesis:
//   - Struct paths: the build pass writes SampleStruct values directly into
//     array slots (no per-element allocation), so the array is dense from
//     the first write. Total time should be close to (#0's dispatch time) +
//     (a cheap linear array-fill). Expect numbers close to #3, since #3's
//     "interleaved" stack-only struct construction was already nearly free.
//   - Class paths: the build pass allocates N SampleClass objects
//     back-to-back, same allocation pattern #0's [GlobalSetup] used — so the
//     resulting array should be just as cache-friendly as #0's array (GC
//     bump allocator packs them contiguously). Expect the dispatch pass to
//     land close to #0's class numbers, with the build pass adding
//     allocation + GC cost on top that #0 excluded from the measurement.
//   - Comparing #3 vs #4 for classes isolates whether interleaving
//     construction with dispatch (which pointer-chases through cold,
//     just-allocated objects) is any different from a dense two-pass build:
//     if the array is small enough to stay warm in cache, the two should be
//     close; any gap points at construction/dispatch interleaving defeating
//     something (allocator locality, branch prediction, etc.) that a clean
//     two-pass split does not.
//
// Result (AMD EPYC 9654 2.40GHz, Windows 11, .NET 9.0.16, 2026-07-02):
//
// | Method             | N        | Mean            | Ratio | Allocated     |
// |--------------------|----------|----------------:|------:|--------------:|
// | ViaConcreteStruct  |     1000 |        1.629 us |  1.00 |       7.84 KB |
// | ViaGenericStruct   |     1000 |        1.611 us |  0.99 |       7.84 KB |
// | ViaInterfaceStruct |     1000 |        3.224 us |  1.98 |      31.27 KB |
// | ViaConcreteClass   |     1000 |        4.575 us |  2.81 |      31.27 KB |
// | ViaGenericClass    |     1000 |        4.527 us |  2.78 |      31.27 KB |
// | ViaInterfaceClass  |     1000 |        4.539 us |  2.79 |      31.27 KB |
// | ViaConcreteStruct  |   100000 |      292.376 us |  1.00 |     781.36 KB |
// | ViaGenericStruct   |   100000 |      294.508 us |  1.01 |     781.36 KB |
// | ViaInterfaceStruct |   100000 |      575.541 us |  1.97 |    3125.10 KB |
// | ViaConcreteClass   |   100000 |    1,053.057 us |  3.60 |    3125.10 KB |
// | ViaGenericClass    |   100000 |    1,280.977 us |  4.38 |    3125.10 KB |
// | ViaInterfaceClass  |   100000 |    1,261.224 us |  4.31 |    3125.11 KB |
// | ViaConcreteStruct  | 10000000 |   18,542.722 us |  1.00 |   78125.31 KB |
// | ViaGenericStruct   | 10000000 |   18,162.169 us |  0.98 |   78125.31 KB |
// | ViaInterfaceStruct | 10000000 |   36,254.240 us |  1.96 |  312500.16 KB |
// | ViaConcreteClass   | 10000000 |  239,328.747 us | 12.91 |  312500.02 KB |
// | ViaGenericClass    | 10000000 |  260,679.356 us | 14.07 |  312500.02 KB |
// | ViaInterfaceClass  | 10000000 |  260,684.846 us | 14.07 |  312500.02 KB |
//
// Summary:
//
//   Struct paths behave exactly as hypothesized: ViaConcreteStruct and
//   ViaGenericStruct match each other at every N, and ViaInterfaceStruct
//   holds a flat ~1.96-1.98x ratio (the boxing cost at the Interface()
//   dispatch, same mechanism as #0/#3) at N=1000, N=100000, *and*
//   N=10,000,000. No cliff, no surprises — a dense array-fill followed by
//   a linear read scales the way you'd expect.
//
//   Notably, the two-pass build-then-dispatch shape costs roughly 2x more
//   for structs than #3's interleaved build-and-consume: ViaConcreteStruct
//   is 18.54 ms here vs. 8.53 ms in #3 at N=10,000,000. The reason is
//   structural, not a struct-vs-class effect — #4 forces two full
//   sequential passes over an up-to-80 MB array (one write during the
//   build loop, one read during the dispatch loop), while #3 never
//   materializes an array at all: each SampleStruct is a transient local
//   produced and immediately consumed, likely never leaving a register.
//
//   Class paths tell the real story. At N=1000/100000 the ratios are
//   modest (2.8x-4.4x), similar order of magnitude to #3's construction
//   cost. But at N=10,000,000 all three class variants blow up to
//   ~12.9x-14.1x (239-261 ms) while struct stays flat at ~2x. This is a
//   genuine cliff that neither #0 (dispatch-only, array built outside
//   the timed region) nor #3 (one instance constructed and consumed at a
//   time) showed. Building a dense SampleStruct[10M] is one ~80 MB linear
//   write; building a SampleClass[10M] is 10 million individual small
//   heap allocations, each needing a bump-pointer check and periodic Gen0
//   collection — and because the array keeps every instance reachable for
//   the whole benchmark, they get promoted rather than collected young.
//   At this scale the per-allocation cost stops being proportional to N
//   and starts compounding.
//
// Major takeaway:
//
//   0_StructVsClassBenchmarks found that *reading* a large SampleClass[]
//   array (built once, outside the timed region) shows no dense-vs-sparse
//   cliff at all — sequential access hides the pointer-chase and the
//   bump allocator packs instances contiguously. This file shows that
//   finding was never in question — the cliff was never in the reading.
//   It's entirely in the *building*. Once a SampleClass[10M] exists,
//   walking it is just as fast as walking a struct array (per #0);
//   building it in the first place is ~13x more expensive than building
//   the equivalent struct array (per #4), and that gap only shows up at
//   scale — it's invisible at N<=100,000. A "build a big batch of class
//   instances" call site should expect that cost to show up specifically
//   under load, not in a small-scale smoke test.
[MemoryDiagnoser]
public class StructVsClassArrayConstructionBenchmarks
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
        var a = _aValues;
        var b = _bValues;
        var samples = new SampleStruct[a.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = new SampleStruct { A = a[i], B = b[i] };
        }

        float total = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            total += ConcreteStruct(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericStruct()
    {
        var a = _aValues;
        var b = _bValues;
        var samples = new SampleStruct[a.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = new SampleStruct { A = a[i], B = b[i] };
        }

        float total = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Generic(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceStruct()
    {
        var a = _aValues;
        var b = _bValues;
        var samples = new SampleStruct[a.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = new SampleStruct { A = a[i], B = b[i] };
        }

        float total = 0;
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
        var a = _aValues;
        var b = _bValues;
        var samples = new SampleClass[a.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = new SampleClass { A = a[i], B = b[i] };
        }

        float total = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            total += ConcreteClass(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaGenericClass()
    {
        var a = _aValues;
        var b = _bValues;
        var samples = new SampleClass[a.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = new SampleClass { A = a[i], B = b[i] };
        }

        float total = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Generic(samples[i]);
        }
        return total;
    }

    [Benchmark]
    public float ViaInterfaceClass()
    {
        var a = _aValues;
        var b = _bValues;
        var samples = new SampleClass[a.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = new SampleClass { A = a[i], B = b[i] };
        }

        float total = 0;
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
