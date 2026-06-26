using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks;

// Minimal reproduction of the interface-argument-vs-generic-argument cost we
// hit in Building.ComputeComponents. The shape mirrors the real call site:
//   - a value-type "payload" implementing a small interface
//   - a hot loop that reads two properties off the payload and does cheap math
// The only thing that varies across benchmarks is *how the payload is passed*.
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method       | N      | Mean         | Ratio | Allocated  |
// |--------------|--------|--------------|-------|------------|
// | ViaConcrete  | 1000   |     551.5 ns |  1.00 |          - |
// | ViaGeneric   | 1000   |     549.8 ns |  1.00 |          - |
// | ViaInterface | 1000   |   2,469.3 ns |  4.48 |   24,000 B |
// | ViaConcrete  | 100000 |  60,193.4 ns |  1.00 |          - |
// | ViaGeneric   | 100000 |  60,221.9 ns |  1.00 |          - |
// | ViaInterface | 100000 | 250,204.9 ns |  4.16 | 2,400,000 B|
//
// Generic and concrete come out within noise here: the JIT specializes
// Generic<SampleStruct> for the value type and the body is small enough to
// inline. With a non-inlineable body the absolute numbers would shift, but
// nothing would box on the generic path. The interface path pays ~4-5x in
// time and one allocation per element — the struct gets boxed for each
// ISample parameter and A/B turn into virtual interface dispatches. The
// boxing is the cleanest evidence; the time delta scales with body size.
[MemoryDiagnoser]
public class InterfaceVsGenericBenchmarks
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

    // Generic constraint. The JIT specializes Generic<SampleStruct> for the
    // value-type T, so property access stays direct — no boxing, no vtable.
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

    // Interface parameter. Passing a struct as ISample boxes it on every call
    // and turns the property reads into virtual interface dispatches.
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

    private static float Generic<T>(T s) where T : ISample => s.A * 2f + s.B;

    private static float Interface(ISample s) => s.A * 2f + s.B;
}
