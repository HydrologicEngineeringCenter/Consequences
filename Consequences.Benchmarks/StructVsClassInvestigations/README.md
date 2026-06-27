# Struct vs. Class Investigations

A follow-up to the [Interface Investigations](../InterfaceInvestigations/README.md).
That suite measured the cost of passing a value-type payload through different
abstractions (concrete, generic, interface). One of the open follow-ups it left
behind was:

> **Class implementation, sealed, held as interface.** Replace structs with
> `sealed class`. No receiver boxing, and tiered PGO should
> guarded-devirtualize. Answers "should we just use classes?"

This folder answers that question. We add a `BuildingClass` that is identical
to `Building` in every way except `struct` → `class`, and re-run the same
three call shapes (`Concrete` / `Generic` / `Interface`) side-by-side against
both implementations.

## Benchmarks

| File | What it measures |
|---|---|
| `0_StructVsClassBenchmarks.cs` | Toy `SampleStruct` vs. `SampleClass` with a one-multiply body. Isolates dispatch cost from per-call work — the place struct-vs-class differences are most visible. |
| `1_BuildingStructVsClassBenchmarks.cs` | Production types: `Building` (struct) vs. `BuildingClass` (class), both implementing `IConsequenceReceptor<DepthHazard, DamageResult>`, with the real `Compute` body. Single receptor reused per call — stresses the hazard stream, not the receptor layout. |
| `2_BuildingArrayStructVsClassBenchmarks.cs` | Same production types and call shapes as #1, but holds `Building[N]` / `BuildingClass[N]` and walks one element per hazard. Designed to expose the dense-vs-sparse cache cliff that #1 could not. |

Each file runs six methods: `ViaConcreteStruct`, `ViaGenericStruct`,
`ViaInterfaceStruct`, `ViaConcreteClass`, `ViaGenericClass`,
`ViaInterfaceClass`.

Run them with:

```bash
dotnet run -c Release --project Consequences.Benchmarks -- \
  --filter '*StructVsClassInvestigations*'
```

## Combined results (Apple M4, .NET 9.0.8)

### Toy benchmark (`0_StructVsClassBenchmarks`)

Ratios are vs. `ViaConcreteStruct` at the same N. N=10M was added to push the
sample array past M4's 16 MB perf-core L2.

| Method             | N = 1k       | N = 100k       | N = 10M (L2-pressure)        |
|--------------------|-------------:|---------------:|-----------------------------:|
| ViaConcreteStruct  |   542 ns  (1.00) |   59,492 ns  (1.00) |    6,101,856 ns  (1.00)      |
| ViaGenericStruct   |   539 ns  (0.99) |   59,356 ns  (1.00) |    6,156,951 ns  (1.01)      |
| ViaInterfaceStruct | 2,467 ns  (4.55, 24 KB alloc) | 245,484 ns (4.13, 2.4 MB) | 27,242,684 ns (4.46, **240 MB**) |
| ViaConcreteClass   |   586 ns  (1.08) |   64,178 ns  (1.08) |    6,778,718 ns  (1.11)      |
| ViaGenericClass    |   610 ns  (1.13) |   66,129 ns  (1.11) |    6,858,129 ns  (1.12)      |
| ViaInterfaceClass  |   612 ns  (1.13) |   66,527 ns  (1.12) |    6,851,233 ns  (1.12)      |

### Production benchmark (`1_BuildingStructVsClassBenchmarks`)

| Method             | N = 1k         | N = 100k         | N = 10M (L2-pressure)   |
|--------------------|---------------:|-----------------:|------------------------:|
| ViaConcreteStruct  |   9.112 µs  (1.00) |   1,183.7 µs  (1.00) |   121,162 µs  (1.00)    |
| ViaGenericStruct   |   9.131 µs  (1.00) |   1,157.4 µs  (0.98) |   120,476 µs  (0.99)    |
| ViaInterfaceStruct |   9.250 µs  (1.02) |   1,183.3 µs  (1.00) |   122,888 µs  (1.01)    |
| ViaConcreteClass   |   9.273 µs  (1.02) |   1,172.8 µs  (0.99) |   123,151 µs  (1.02)    |
| ViaGenericClass    |   9.254 µs  (1.02) |   1,164.6 µs  (0.98) |   123,281 µs  (1.02)    |
| ViaInterfaceClass  |   9.514 µs  (1.04) |   1,232.3 µs  (1.04) |   128,471 µs  (1.06)    |

All paths in the production benchmark allocate nothing at every N.

### Receptor-array benchmark (`2_BuildingArrayStructVsClassBenchmarks`)

This benchmark replaces the single reused receptor with `Building[N]` /
`BuildingClass[N]`, so the receptor itself contributes to the working set.
At N=10M that's ~400 MB dense (struct array) vs. ~640 MB sparse (class
references + heap objects), both deep into main memory.

| Method             | N = 1k          | N = 100k         | N = 10M (receptor array past L2) |
|--------------------|----------------:|-----------------:|---------------------------------:|
| ViaConcreteStruct  |  10.164 µs  (1.00) |   1,270.3 µs  (1.00) |   129,466 µs  (1.00) |
| ViaGenericStruct   |   9.690 µs  (0.95) |   1,272.3 µs  (1.00) |   129,704 µs  (1.00) |
| ViaInterfaceStruct |   9.839 µs  (0.97) |   1,269.4 µs  (1.00) |   130,632 µs  (1.01) |
| ViaConcreteClass   |  10.283 µs  (1.01) |   1,290.6 µs  (1.02) |   130,412 µs  (1.01) |
| ViaGenericClass    |  10.202 µs  (1.00) |   1,293.3 µs  (1.02) |   133,429 µs  (1.03) |
| ViaInterfaceClass  |  10.194 µs  (1.00) |   1,262.2 µs  (0.99) |   131,515 µs  (1.02) |

All paths allocate nothing per op at every N.

## What happened

**Toy benchmark — the structural story.** With a one-multiply body, the cost
of each call shape is visible instead of being buried under per-call work.

For the *struct* group we reproduce the result from
`InterfaceVsGenericBenchmarks`: `Concrete` and `Generic` are identical (the JIT
specializes for the value type and inlines), `Interface` is ~4x slower and
allocates one box per call (`SampleStruct` boxed at the `ISample` parameter
slot).

For the *class* group all three paths land within ~5% of each other and
within ~10–14% of the struct baseline. The reasons differ per row but the
shape is consistent:

- **ViaConcreteClass.** A class is a heap object, so accessing `A`/`B` costs
  one extra indirection per read. The body is too small for the JIT to fully
  hide this. ~10% over the struct baseline.
- **ViaGenericClass.** Reference-sharing kicks in: the JIT emits one compiled
  body for all reference Ts and uses constrained-virtual dispatch through the
  `MethodTable`. There is no per-class specialization win the way there was
  per-struct.
- **ViaInterfaceClass.** No boxing — the reference is already exactly what
  `ISample` wants. Just a virtual call, which tiered PGO can mostly
  devirtualize when only one implementation is in flight.

The two non-obvious numbers in the toy table are:

1. **`ViaGenericClass` ≈ `ViaInterfaceClass`** — *not* `ViaConcreteClass`. The
   per-struct specialization that made `ViaGenericStruct` collapse onto
   `ViaConcreteStruct` does not have an equivalent for reference types. So
   "generic over a class" is essentially "interface dispatch on a class," not
   "concrete call on a class."
2. **`ViaInterfaceClass` ≈ `ViaConcreteClass` (within ~5%)**. The 4x cost
   `ViaInterfaceStruct` paid was almost entirely *the box*, not the virtual
   dispatch. Move the payload to the heap and the dispatch alone is nearly
   free.

**Production benchmark — what survives.** Once `Compute` does real work
(curve interpolation, value scaling), all six paths fall within ~6% of each
other and none allocate. The per-call body (~12 ns of arithmetic and two
interpolations) swamps any per-call dispatch difference. The
struct's `ViaInterface` does not allocate per call because the struct is
boxed *once* at assignment to the interface-typed local — and because
`THazard`/`TResult` are concrete value types in the signature, the per-call
payload (`DepthHazard`, `DamageResult`) doesn't box either. That was the
mechanism `GenericComputerInterfaceBenchmarks` established and it carries
through here unchanged.

The only mildly slower row is `ViaInterfaceClass` (~4–6%), which is
consistent with virtual dispatch on a class instance being slightly harder
for PGO to guard-devirtualize than dispatch through a single boxed struct
(where there is exactly one reference identity in the entire program).

**L2-pressure regime (N = 10M).** All three benchmarks were extended to
N=10M to push working sets past M4's 16 MB perf-core L2:

- Toy: `SampleStruct[10M]` = 80 MB dense; `SampleClass[10M]` = 80 MB of refs
  plus ~320 MB of heap objects. Both well over L2.
- Production (`#1`): `DepthHazard[10M]` = 40 MB hazard stream. Over L2 by
  ~2.5x. Receptor itself was a single hot instance.
- Receptor array (`#2`): `Building[10M]` ≈ 400 MB dense; `BuildingClass[10M]`
  ≈ 640 MB sparse (refs + heap objects). The receptor itself now
  contributes to the working set. This is the variant the previous round
  flagged as missing.

Across all three benchmarks **every ratio at N=10M is within ~3% of the
ratio at N=100k**. The dense-struct-array-vs-array-of-references cache
cliff did not materialize, even in the receptor-array variant where it had
the strongest opportunity to. Reasons:

1. **Sequential access.** All benchmarks walk arrays front-to-back with
   unit stride. The hardware prefetcher walks ahead of the demand load, so
   L2/L3 misses are serviced before they can stall the pipeline.
2. **Allocation locality.** `SampleClass` / `BuildingClass` instances are
   allocated back-to-back during `Setup`, so the GC's bump allocator packs
   them in consecutive heap pages. The "sparse" array-of-references is, in
   practice, almost as cache-coherent as the dense struct array on the
   first walk.
3. **Body large enough to amortize.** ~13 ns of per-call work (curve
   interpolation + value scaling) gives the prefetcher all the runway it
   needs. The walk is memory-touched, not memory-*bound*.

What N=10M *did* expose:

- `ViaInterfaceStruct` allocates **240 MB per operation** at the toy level.
  The GC absorbs it (no extra latency vs. N=100k after normalizing by N),
  but as an absolute number it's striking — the per-call box is real, it's
  just normally short enough for Gen0 to collect before it shows up.
- The production benchmarks scale perfectly linearly from N=100k to N=10M.
  Per-call cost did not grow as either the hazard stream or the receptor
  array spilled out of cache.

To actually trigger the cliff would require one of: (a) random or strided
access that defeats the prefetcher, (b) a per-element body so small that
miss latency dominates total time, or (c) per-instance data large enough to
thrash regardless of layout. For Building's real workload, none of those
hold — the struct-vs-class choice is performance-neutral and the random-
access variant is the next test if we want to push further.

## Conclusion

Re-reading the Interface investigations with these numbers in hand: the 4x
penalty `ViaInterface` showed in the toy benchmarks was overwhelmingly the
cost of *boxing a struct*, not the cost of *virtual dispatch*. Switching the
payload from struct to class eliminates the box entirely, and the residual
virtual call is small enough that PGO can usually hide it.

That gives a sharper version of the previous conclusion:

> When a value type passes through an interface-typed parameter, each call
> boxes. The remedy is either to push the value type concretely through the
> signature (`IComputer<TIn, TOut>` with constrained struct args) **or** to
> use a class — both eliminate the box, for different reasons.

For `Building` specifically: on the real workload, struct vs. class is a
wash. The struct choice should be defended on grounds *other* than hot-path
speed:

- value semantics and copy-on-assignment (no aliasing surprises),
- dense, cache-friendly layout for `Building[]`,
- zero GC pressure for arrays of building data.

If `Building` were being held primarily behind `IConsequenceReceptor<...>`
*and* the per-call body were small, a class would be slightly faster (no box
on each interface-typed parameter pass). Neither condition currently
applies, so the struct stays.

## Things worth investigating next

- **Random-access receptor array.** Same setup as
  `2_BuildingArrayStructVsClassBenchmarks`, but the loop indexes through a
  shuffled index array instead of walking sequentially. Defeats the
  hardware prefetcher and isolates the pointer-chase cost of
  array-of-references. This is the test most likely to actually surface
  the dense-vs-sparse gap; the sequential variant came back flat.
- **Smaller per-call body.** Replace `OrderedPairedData.GetYFromX` with a
  trivial scaling so the per-element work drops from ~13 ns to ~1 ns.
  Should make memory-miss latency dominant and let the layout difference
  show. Conversely, useful as a sanity check that the flat result in #2
  really is "compute-bound, prefetcher hides it" and not "JIT is
  optimizing the difference away."
- **`sealed class BuildingClass`.** Should reliably nudge PGO into
  unconditional devirtualization on the `ViaInterfaceClass` path.
  Quantifies the "PGO can't lock in" theory for the ~4–6% gap on
  `ViaInterfaceClass` in `1_BuildingStructVsClassBenchmarks`.
- **Mixed array.** A loop holding `IConsequenceReceptor<...>[]` where some
  entries are `Building` (boxed) and others are `BuildingClass` (already
  heap). Tests whether the polymorphic call site degrades both paths
  together — PGO can guard-devirtualize one implementation but probably
  not two.
- **`readonly struct` vs. `struct`.** `Building` is currently `struct` (not
  `readonly struct`). Switching it typically lets the JIT skip defensive
  copies on property reads in `in`-parameter scenarios. Worth a
  before/after on the production benchmark.
- **Fragmented heap.** Allocate `BuildingClass` instances interleaved with
  other long-lived garbage so they aren't contiguous. The N=10M class
  results stayed flat partly because Setup allocates them back-to-back —
  test whether that survives realistic allocation patterns.
