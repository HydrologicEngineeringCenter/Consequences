using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks;

// Variant of InterfaceVsGenericBenchmarks where ISample is a *marker* interface
// (no members). Because the abstraction exposes nothing, the Generic and
// Interface paths have to recover the concrete type to read A/B — which lets
// us measure the cost of the marker-style dispatch vs. a direct call.
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method       | N      | Mean         | Ratio | Allocated  |
// |--------------|--------|--------------|-------|------------|
// | ViaConcrete  | 1000   |     557.3 ns |  1.00 |          - |
// | ViaGeneric   | 1000   |     552.8 ns |  0.99 |          - |
// | ViaInterface | 1000   |   2,469.1 ns |  4.43 |   24,000 B |
// | ViaConcrete  | 100000 |  60,315.2 ns |  1.00 |          - |
// | ViaGeneric   | 100000 |  60,294.3 ns |  1.00 |          - |
// | ViaInterface | 100000 | 246,071.8 ns |  4.08 | 2,400,000 B|
//
// Numbers are nearly identical to the non-marker version: emptying the
// interface didn't change anything. Caveat: the Generic body uses
// `s is SampleStruct ss`, so this measures generic dispatch plus a type
// test, not pure generic dispatch. The type test should fold during
// per-struct specialization, but that's a JIT heuristic, not a guarantee.
// The Interface path still pays the boxing + unbox-cast cost on every call.
[MemoryDiagnoser]
public class MarkerInterfaceVsGenericBenchmarks
{
    public interface ISample
    {
    }

    public struct SampleStruct : ISample
    {
        public float A { get; set; }
        public float B { get; set; }
    }

    [Params(1_000, 100_000)]
    public int N;

    private SampleStruct[] _samples = [];

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _samples = new SampleStruct[N];
        for (int i = 0; i < N; i++)
        {
            _samples[i] = new SampleStruct { A = (float)rng.NextDouble(), B = (float)rng.NextDouble() };
        }
    }

    // Lower bound: no abstraction at all. The JIT sees the concrete struct
    // and inlines the property reads.
    [Benchmark(Baseline = true)]
    public float ViaConcrete()
    {
        float total = 0;
        var samples = _samples;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Concrete(samples[i]);
        }
        return total;
    }

    // Generic constraint against the marker interface. The JIT still
    // specializes for the value-type T, but with no members on ISample we
    // have to pattern-match back to SampleStruct to do the math.
    [Benchmark]
    public float ViaGeneric()
    {
        float total = 0;
        var samples = _samples;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Generic(samples[i]);
        }
        return total;
    }

    // Interface parameter. Passing a struct as ISample boxes it on every
    // call; recovering A/B then requires unboxing back to SampleStruct.
    [Benchmark]
    public float ViaInterface()
    {
        float total = 0;
        var samples = _samples;
        for (int i = 0; i < samples.Length; i++)
        {
            total += Interface(samples[i]);
        }
        return total;
    }

    private static float Concrete(SampleStruct s) => s.A * 2f + s.B;

    private static float Generic<T>(T s) where T : ISample
        => s is SampleStruct ss ? ss.A * 2f + ss.B : 0f;

    private static float Interface(ISample s)
    {
        var ss = (SampleStruct)s;
        return ss.A * 2f + ss.B;
    }
}
