using BenchmarkDotNet.Attributes;

namespace Consequences.Benchmarks;

// Three-interface variant. IInput and IOutput are bare marker interfaces;
// IComputer exposes a single method that takes an IInput and returns an
// IOutput. We compare the same three call shapes as the other benchmarks:
// concrete static call, generic-constrained call, and interface call.
//
// Result (Apple M4, .NET 9.0.8, 2026-06-26):
//
// | Method       | N      | Mean         | Ratio | Allocated   |
// |--------------|--------|--------------|-------|-------------|
// | ViaConcrete  | 1000   |     528.9 ns |  1.00 |           - |
// | ViaGeneric   | 1000   |   4,818.3 ns |  9.11 |    48,000 B |
// | ViaInterface | 1000   |   4,828.4 ns |  9.13 |    48,000 B |
// | ViaConcrete  | 100000 |  58,324.2 ns |  1.00 |           - |
// | ViaGeneric   | 100000 | 483,365.1 ns |  8.29 | 4,800,000 B |
// | ViaInterface | 100000 | 479,965.7 ns |  8.23 | 4,800,000 B |
//
// The Generic path collapses onto the Interface path — both ~9x slower than
// concrete, both allocating two boxes per call (the IInput argument and the
// IOutput return). Specializing the *computer* on a value-type TComputer
// doesn't help when the method it dispatches to is `IOutput Compute(IInput)`:
// the signature itself forces boxing at the boundary, regardless of how the
// computer is held.
[MemoryDiagnoser]
public class ComputerInterfaceBenchmarks
{
    public interface IInput
    {
    }

    public interface IOutput
    {
    }

    public interface IComputer
    {
        IOutput Compute(IInput input);
    }

    public struct Input : IInput
    {
        public float Value { get; set; }
    }

    public struct Output : IOutput
    {
        public float Value { get; set; }
    }

    public struct Computer : IComputer
    {
        public IOutput Compute(IInput input)
        {
            var i = (Input)input;
            return new Output { Value = i.Value * 2f };
        }
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

    // Lower bound: no abstraction. Direct static call on concrete struct types.
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

    // Generic constraint on the computer. The JIT can specialize for the
    // value-type TComputer, but the IComputer signature still forces the
    // input to be boxed as IInput and the result to be returned as IOutput.
    [Benchmark]
    public float ViaGeneric()
    {
        float total = 0;
        var inputs = _inputs;
        var computer = new Computer();
        for (int i = 0; i < inputs.Length; i++)
        {
            var output = Generic(computer, inputs[i]);
            total += ((Output)output).Value;
        }
        return total;
    }

    // Interface all the way down. Computer is held as IComputer, input is
    // boxed on every call, output comes back boxed as IOutput.
    [Benchmark]
    public float ViaInterface()
    {
        float total = 0;
        var inputs = _inputs;
        IComputer computer = new Computer();
        for (int i = 0; i < inputs.Length; i++)
        {
            var output = computer.Compute(inputs[i]);
            total += ((Output)output).Value;
        }
        return total;
    }

    private static Output Concrete(Input input) => new() { Value = input.Value * 2f };

    private static IOutput Generic<TComputer>(TComputer computer, IInput input)
        where TComputer : IComputer
        => computer.Compute(input);
}
