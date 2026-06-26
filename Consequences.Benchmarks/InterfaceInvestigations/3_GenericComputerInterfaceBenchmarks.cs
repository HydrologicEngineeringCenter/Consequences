using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks;

// Follow-up to ComputerInterfaceBenchmarks. Same three-interface shape, but
// IComputer is now generic in its input and output types:
//   IComputer<TIn, TOut> where TIn : struct, IInput, TOut : struct, IOutput
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method       | N      | Mean        | Ratio | Allocated |
// |--------------|--------|-------------|-------|-----------|
// | ViaConcrete  | 1000   |    528.7 ns |  1.00 |         - |
// | ViaGeneric   | 1000   |    529.0 ns |  1.00 |         - |
// | ViaInterface | 1000   |    523.7 ns |  0.99 |         - |
// | ViaConcrete  | 100000 | 58,138.7 ns |  1.00 |         - |
// | ViaGeneric   | 100000 | 58,584.6 ns |  1.01 |         - |
// | ViaInterface | 100000 | 56,137.5 ns |  0.97 |         - |
//
// All three paths land within noise of each other and allocate nothing — a
// dramatic reversal from the non-generic ComputerInterfaceBenchmarks (where
// Generic and Interface were both ~8x slower with 4.8 MB allocated at N=100k).
// The fix is exactly what was predicted: with TIn/TOut as concrete value
// types in the interface signature, there's no parameter-boundary erasure,
// so no per-call boxing.
//
// The 0.97x on ViaInterface should be read as "within noise of Concrete,"
// not as actually faster. Plausible mechanisms: tiered PGO guarded-
// devirtualizing the single seen implementation, and/or a body small enough
// that any residual dispatch overhead is lost in the loop. Either way, the
// load-bearing observation is the absence of allocations, not the sub-1.0
// time ratio.
[MemoryDiagnoser]
public class GenericComputerInterfaceBenchmarks
{
    public interface IInput
    {
    }

    public interface IOutput
    {
    }

    public interface IComputer<TIn, TOut>
        where TIn : struct, IInput
        where TOut : struct, IOutput
    {
        TOut Compute(TIn input);
    }

    public struct Input : IInput
    {
        public float Value { get; set; }
    }

    public struct Output : IOutput
    {
        public float Value { get; set; }
    }

    public struct Computer : IComputer<Input, Output>
    {
        public Output Compute(Input input) => new() { Value = input.Value * 2f };
    }

    [Params(1_000, 100_000)]
    public int N;

    private Input[] _inputs = [];

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _inputs = new Input[N];
        for (int i = 0; i < N; i++)
        {
            _inputs[i] = new Input { Value = (float)rng.NextDouble() };
        }
    }

    [Benchmark(Baseline = true)]
    public float ViaConcrete()
    {
        float total = 0;
        var inputs = _inputs;
        for (int i = 0; i < inputs.Length; i++)
        {
            total += Concrete(inputs[i]).Value;
        }
        return total;
    }

    // Generic constraint all the way through: TComputer is a value type, TIn
    // and TOut are concrete value types. The JIT specializes the whole chain.
    [Benchmark]
    public float ViaGeneric()
    {
        float total = 0;
        var inputs = _inputs;
        var computer = new Computer();
        for (int i = 0; i < inputs.Length; i++)
        {
            total += Generic<Computer, Input, Output>(computer, inputs[i]).Value;
        }
        return total;
    }

    // Holding the computer as IComputer<Input, Output>. The struct gets boxed
    // once at assignment, so per-call we pay interface dispatch on Compute,
    // but TIn/TOut are concrete value types — no per-call boxing of the
    // payload.
    [Benchmark]
    public float ViaInterface()
    {
        float total = 0;
        var inputs = _inputs;
        IComputer<Input, Output> computer = new Computer();
        for (int i = 0; i < inputs.Length; i++)
        {
            total += computer.Compute(inputs[i]).Value;
        }
        return total;
    }

    private static Output Concrete(Input input) => new() { Value = input.Value * 2f };

    private static TOut Generic<TComputer, TIn, TOut>(TComputer computer, TIn input)
        where TComputer : IComputer<TIn, TOut>
        where TIn : struct, IInput
        where TOut : struct, IOutput
        => computer.Compute(input);
}
