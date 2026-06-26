# Interface Investigations

A small suite of BenchmarkDotNet experiments measuring the runtime cost of
different ways of passing value-type data through abstractions. The shape
mirrors the real call sites in `Building.ComputeComponents` and similar hot
loops: a small value type flows into a function and we sum a cheap
computation over an array.

The benchmarks vary in *what the abstraction exposes*:

| File | Interface shape | What we're measuring |
|---|---|---|
| `0_InterfaceVsGenericBenchmarks.cs` | `ISample` exposes `A`/`B` properties | Baseline: a "normal" small interface |
| `1_MarkerInterfaceVsGenericBenchmarks.cs` | `ISample` is empty (marker) | Does emptying the interface change anything? |
| `2_ComputerInterfaceBenchmarks.cs` | `IComputer.Compute(IInput) -> IOutput`, both markers | What if the interface method takes/returns *other* interfaces? |
| `3_GenericComputerInterfaceBenchmarks.cs` | `IComputer<TIn, TOut>` with concrete struct args | Does making the interface itself generic fix it? |
| `4_BuildingGenericComputerBenchmarks.cs` | `IConsequenceReceptor<THazard, TResult>` on real `Building` / `DepthHazard` / `DamageResult` | Does the pattern survive on real production types whose `Compute` does actual work? |

Each file runs the same three benchmarks:

- `ViaConcrete` — direct static call on the concrete struct (lower bound)
- `ViaGeneric` — generic method with `where T : ...` constraint
- `ViaInterface` — parameter (or holder) typed as the interface

Run them with:

```bash
dotnet run -c Release --project Consequences.Benchmarks -- \
  --filter '*InterfaceVsGenericBenchmarks*' '*MarkerInterfaceVsGenericBenchmarks*' \
           '*ComputerInterfaceBenchmarks*' '*GenericComputerInterfaceBenchmarks*'
```

## Combined results (Apple M4, .NET 9.0.8, N = 100,000)

| Benchmark | Concrete | Generic | Interface |
|---|---:|---:|---:|
| `InterfaceVsGeneric` (interface exposes A/B) |  60.2 µs (1.00x) |  60.2 µs (**1.00x**) | 250.2 µs (4.16x, 2.4 MB alloc) |
| `MarkerInterfaceVsGeneric` (empty interface)  |  60.3 µs (1.00x) |  60.3 µs (**1.00x**) | 246.1 µs (4.08x, 2.4 MB alloc) |
| `ComputerInterface` (method takes IInput / returns IOutput) |  58.3 µs (1.00x) | 483.4 µs (**8.29x**, 4.8 MB) | 480.0 µs (8.23x, 4.8 MB) |
| `GenericComputerInterface` (`IComputer<TIn, TOut>`) |  58.1 µs (1.00x) |  58.6 µs (**1.01x**) |  56.1 µs (≈1.00x †) |

† The `GenericComputerInterface` Interface row comes back at 0.97x but should
be read as "within noise of Concrete," not as actually faster. With a tiny
body and a single seen implementation, tiered PGO can guarded-devirtualize
the call and any residual dispatch overhead drops below loop noise. The
load-bearing observation is the absence of allocations, not the sub-1.0 time
ratio.

The allocation column is the cleanest evidence: every row that's "slow" also
allocates per call (24 B or 48 B × N elements); every row that's "fast"
allocates nothing. The last row — generic all the way through, including the
interface itself — collapses *all three paths* back onto the concrete
baseline with zero allocations.

## What happened

The first two benchmarks confirm what's been folklore for years: when a
generic method is constrained to an interface and called with a value type,
the JIT specializes the method body for that value type. Property reads
inline, no boxing occurs, the call is indistinguishable from a hand-rolled
concrete one. Whether the interface exposes members or is an empty marker
doesn't matter — the generic specialization happens at the *type* level, not
at the member level.

The third benchmark breaks that pattern by moving the abstraction *inside*
the method signature. `IComputer.Compute` is declared to take `IInput` and
return `IOutput`. Even when the *computer* is held as a generic-specialized
value type, every call site still has to:

1. Box the `Input` struct to satisfy the `IInput` parameter slot.
2. Receive an `IOutput` reference back, then unbox-cast to `Output`.

The JIT cannot specialize through that signature — `IInput` and `IOutput`
are the declared types and there is no `T` to bind. Result: two heap
allocations per call (48 bytes × 100,000 = 4.8 MB allocated). The fact that
the *holder* of `IComputer` is generic is irrelevant — boxing happens at the
parameter boundary.

The fourth benchmark proves the fix: parameterize the interface itself.
`IComputer<TIn, TOut>` with `TIn : struct, IInput` and `TOut : struct,
IOutput` lets a value-type computer expose a concretely-typed
`Output Compute(Input)`. Now there is no erasure at any boundary, and all
three call shapes (Concrete, Generic, even Interface-held) land within
benchmark noise of each other with zero allocations.

## Conclusion

Initial conclusion was:

> "We cannot have generic ins and outs either as true generics or interfaces
> without suffering a significant speed penalty."

Refined and now backed by the `GenericComputerInterface` data:

> When a value type passes through an **interface-typed parameter or
> return**, each call boxes — even if the caller is generic. The remedy in
> hot loops is to push the type through the signature, either concretely or
> by parameterizing the interface itself (`IComputer<TIn, TOut>`). Generic
> ins and outs are free *as long as you don't erase them to an interface
> type mid-flow*.

A couple of caveats worth keeping in mind:

- This is "free" for **value-type** type parameters because the JIT emits a
  per-struct specialization. For **reference-type** `T`, .NET uses *shared
  generics* — one code body across all reference Ts, with runtime
  `MethodTable` lookups for virtual calls on `T`. A generic-all-the-way
  `IComputer<TIn, TOut>` is free for struct args but partially regresses if
  the same hot path is also instantiated with classes.
- "Interface-typed" is not automatically doomed. On .NET 9 with tiered PGO,
  an *interface-typed holder* of a sealed/single class can be
  guarded-devirtualized. The hard rule is narrower: **interface-typed
  parameters/returns of value types always box**.

## Production-types validation (Apple M4, .NET 9.0.8, 2026-06-26)

The toy benchmarks above use a one-multiply `Compute` body. To check whether
the result still holds when `Compute` does real work, `BuildingGenericComputerBenchmarks`
applies the same three call shapes to the actual production types:

- Receptor: `Building` (struct) implementing `IConsequenceReceptor<DepthHazard, DamageResult>`
- Input: `DepthHazard` (struct, marker `IHazard`)
- Output: `DamageResult` (readonly record struct, marker `IConsequenceResult`)

Each `Compute` call does a foundation-height subtraction, two value scalings,
and two `OrderedPairedData.GetYFromX` linear-interpolation lookups against
depth-damage curves — roughly 12 ns of real work per call at N=100k.

| Method       | N      | Mean         | Ratio | Allocated |
|--------------|--------|-------------:|------:|----------:|
| ViaConcrete  |  1,000 |     9.100 µs |  1.00 |         - |
| ViaGeneric   |  1,000 |     9.113 µs |  1.00 |         - |
| ViaInterface |  1,000 |     9.276 µs |  1.02 |         - |
| ViaConcrete  | 100,000 | 1,166.5 µs |  1.00 |         - |
| ViaGeneric   | 100,000 | 1,182.2 µs |  1.01 |         - |
| ViaInterface | 100,000 | 1,195.2 µs |  1.02 |         - |

Verdict: the pattern holds. All three call shapes land within 2% of each
other, and every row allocates nothing. The 1–2% gap on `ViaInterface`
(roughly 0.3 ns per call) is consistent with a single residual interface
dispatch on `Compute`; with the receptor held as the generic interface, the
struct boxes *once* at assignment and is then called per-element through that
single dispatch — payload (`DepthHazard` / `DamageResult`) never boxes because
`THazard` / `TResult` are concrete in the signature.

The architectural read: exposing `IConsequenceReceptor<THazard, TResult>` as
the public abstraction does *not* impose a per-call cost on a realistic
domain method. The boxing failure mode demonstrated by `ComputerInterface`
(method takes `IInput` / returns `IOutput`) does not appear here because the
hazard and result types ride through the signature as their concrete value
types. Generic interfaces with constrained struct parameters are safe to
adopt at the project's computation boundaries.

## Things worth investigating next

- **Validate the `GenericComputerInterface.ViaInterface` result.** Add
  `[MethodImpl(MethodImplOptions.NoInlining)]` to `Computer.Compute` and a
  sink (e.g. `Volatile.Write`) on `total`. If the ratio stays ~1.0x, the
  story is tiered-PGO devirtualization. If it jumps, hoisting or DCE was
  masking the dispatch. Either way, makes the claim defensible.
- **`in` / `ref readonly` parameters** — does `IOutput Compute(in Input)`
  avoid the input box while keeping a non-generic interface? Quantifies the
  "half-fix."
- **Class implementation, sealed, held as interface.** Replace structs with
  `sealed class`. No receiver boxing, and tiered PGO should
  guarded-devirtualize. Answers "should we just use classes?"
- **Reference-type T in the generic interface.** Same generic shape, class
  payloads. Confirms shared-generics overhead is small but nonzero.
- **Larger compute body (8–16 ops).** Verifies the ratio compresses as real
  work grows — important for deciding whether a given call site is worth
  restructuring.
- **Disassembly verification.** `[DisassemblyDiagnoser]` would let us see
  the boxing site directly rather than infer it from allocation counts. Not
  supported on macOS Darwin; would need a Linux or Windows machine to run.
