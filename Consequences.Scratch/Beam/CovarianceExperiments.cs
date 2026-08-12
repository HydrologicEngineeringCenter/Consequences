namespace Consequences.Scratch.Beam;

// =====================================================================================
//  WHY `INsiStructureMapper<out TReceptor>` CANNOT HAVE `TryMap(..., out TReceptor)`
// =====================================================================================
//
//  The real interface in Consequences.Network looks like this:
//
//      public interface INsiStructureMapper<out TReceptor>
//      {
//          TReceptor Map(NsiStructure structure);
//      }
//
//  We wanted to add `bool TryMap(NsiStructure structure, out TReceptor receptor)` so the
//  importer could skip structures it can't map instead of throwing. It won't compile.
//
//  Everything below strips that down to two classes and a struct so you can watch the
//  mechanism directly. Run Demo1 through Demo4 in order.
//
//  The one-line summary, which will make sense by Demo2:
//      `out` means "covariant", covariant means "output positions only",
//      and an `out` parameter is NOT an output position.
//
// =====================================================================================


// ---------- the simplest possible type hierarchy -------------------------------------

public class Animal
{
    public string Name { get; init; } = "some animal";
    public override string ToString() => Name;
}

public class Cat : Animal
{
    public Cat() { Name = "cat"; }
}

public class Dog : Animal
{
    public Dog() { Name = "dog"; }
}

/// <summary>
/// A VALUE type. This one matters more than it looks — <c>Building</c> in the real
/// codebase is a <c>struct</c> too, which is the punchline of Demo3.
/// </summary>
public struct Pebble
{
    public int Grams { get; init; }
    public override string ToString() => $"pebble ({Grams}g)";
}


// ---------- the interfaces under test ------------------------------------------------

/// <summary>
/// The shape the real <c>INsiStructureMapper&lt;out TReceptor&gt;</c> has today.
/// `out` makes T COVARIANT: an <c>IFactory&lt;Cat&gt;</c>; is usable as an <c>IFactory&lt;Animal&gt;</c>.
/// </summary>
public interface IFactory<out T>
{
    T Make();

    // ❌ UNCOMMENT ME to see the whole problem in one compiler error:
    //
    //        bool TryMake(out T item);
    //
    //    error CS1961: Invalid variance: The type parameter 'T' must be invariantly
    //    valid on 'IFactory<T>.TryMake(out T)'. 'T' is covariant.
    //
    //    Demo2 explains why the compiler is right to refuse.
}

/// <summary>
/// The proposed fix: identical, minus the `out`. T is now INVARIANT, so it is legal in
/// input positions, so TryMake compiles. This is exactly the change proposed for
/// INsiStructureMapper.
/// </summary>
public interface IFactoryFixed<T>
{
    T Make();
    bool TryMake(out T item);
}


// ---------- implementations ----------------------------------------------------------

public class CatFactory : IFactory<Cat>
{
    public Cat Make() => new();
}

/// <summary>
/// Mirrors BuildingMapper: it can produce a Pebble, but only for an even number of grams.
/// Odd requests are the stand-in for "occtype I don't recognise".
/// </summary>
public class PebbleFactory : IFactoryFixed<Pebble>
{
    private readonly int _grams;

    public PebbleFactory(int grams) => _grams = grams;

    public Pebble Make() =>
        TryMake(out Pebble pebble)
            ? pebble
            : throw new InvalidOperationException($"{_grams}g is not a pebble I can make.");

    public bool TryMake(out Pebble item)
    {
        if (_grams % 2 != 0)
        {
            item = default;
            return false;
        }

        item = new Pebble { Grams = _grams };
        return true;
    }
}


public static class CovarianceExperiments
{
    public static void RunAll()
    {
        Demo1_WhatCovarianceBuysYou();
        Demo2_WhyOutParametersAreIllegal();
        Demo3_CovarianceDoesNothingForValueTypes();
        Demo4_TheFix();
    }


    // =================================================================================
    //  DEMO 1 — What `out` actually does
    // =================================================================================
    /// <summary>
    /// Covariance = "assignability flows the same direction as the type argument."
    /// A Cat is an Animal, therefore an IFactory&lt;Cat&gt; is an IFactory&lt;Animal&gt;.
    /// </summary>
    public static void Demo1_WhatCovarianceBuysYou()
    {
        Header("DEMO 1 — what the `out` keyword buys you");

        IFactory<Cat> catFactory = new CatFactory();

        // THIS is the only thing `out` enables. Without `out` on IFactory<T>, the next
        // line is a compile error (CS0266).
        IFactory<Animal> asAnimalFactory = catFactory;

        Animal a = asAnimalFactory.Make();
        Console.WriteLine($"  IFactory<Cat> used as IFactory<Animal> -> made a {a}");

        // Why is that safe? Because T only ever comes OUT of the interface. Every Cat
        // that Make() hands back is a valid Animal, so nobody can be surprised.
        Console.WriteLine("  Safe because T only appears in return types — values only flow OUT.");
        Console.WriteLine();
    }


    // =================================================================================
    //  DEMO 2 — Why an `out` parameter breaks that promise
    // =================================================================================
    /// <summary>
    /// The rule: on a covariant T, T may appear ONLY in output positions.
    /// The trap: `out T` is not an output position, despite the keyword.
    /// </summary>
    public static void Demo2_WhyOutParametersAreIllegal()
    {
        Header("DEMO 2 — why `out T` is banned on a covariant interface");

        // First, see the danger covariance-plus-input actually creates. Generics won't let
        // us build it, but ARRAYS are covariant (a C# 1.0 design mistake), so we can run
        // the exact failure that the generic rules exist to prevent:

        Animal[] animals = new Cat[2];      // legal! arrays are covariant
        Console.WriteLine("  Animal[] animals = new Cat[2];   // compiles — arrays are covariant");

        try
        {
            animals[0] = new Dog();          // a Dog IS an Animal... but the array is a Cat[]
            Console.WriteLine("  animals[0] = new Dog();          // ...this should not have worked");
        }
        catch (ArrayTypeMismatchException)
        {
            Console.WriteLine("  animals[0] = new Dog();          // 💥 ArrayTypeMismatchException at RUNTIME");
        }

        Console.WriteLine();
        Console.WriteLine("  That crash is what covariance + an INPUT position costs you.");
        Console.WriteLine("  Generics refuse at COMPILE time instead. Hence: outputs only.");
        Console.WriteLine();

        // So why is `out T item` an input position? It reads like pure output.
        //
        //   - At the IL level, `out T` and `ref T` are the same thing: a BYREF (T&).
        //     `out` is just a C#-level promise that the callee assigns before returning.
        //   - A byref hands the callee a POINTER to the caller's storage location.
        //     The callee can write through it — and a caller holding IFactory<Animal>
        //     over a real IFactory<Cat> would be handing out a Cat-shaped slot for
        //     something to write an Animal into. Same hole as the array above.
        //   - So the CLR treats byref parameters as BOTH input and output. "Both" fails
        //     the outputs-only rule, and the compiler rejects it: CS1961.
        //
        // Uncomment the TryMake line in IFactory<T> above to watch it happen.

        Console.WriteLine("  `out T` is a BYREF (T&) in IL — readable AND writable by the callee,");
        Console.WriteLine("  so the CLR counts it as an input position. Covariance forbids it. CS1961.");
        Console.WriteLine();
    }


    // =================================================================================
    //  DEMO 3 — The `out` was never doing anything for us anyway
    // =================================================================================
    /// <summary>
    /// Variance conversions require an implicit REFERENCE conversion between the type
    /// arguments. Boxing is not a reference conversion, so variance skips value types
    /// entirely. <c>Building</c> is a struct. Therefore the `out` on
    /// <c>INsiStructureMapper</c> is inert for the only receptor that exists.
    /// </summary>
    public static void Demo3_CovarianceDoesNothingForValueTypes()
    {
        Header("DEMO 3 — covariance does nothing for structs");

        // Reference type: variance applies.
        IFactory<Cat> cats = new CatFactory();
        IFactory<Animal> asAnimals = cats;
        Console.WriteLine($"  IFactory<Cat>    -> IFactory<Animal>   ✅  (made a {asAnimals.Make()})");

        // Value type: variance does NOT apply, `out` or no `out`.
        //
        //     IFactory<Pebble> pebbles = ...;
        //     IFactory<object> asObjects = pebbles;
        //
        //     error CS0266: Cannot implicitly convert type 'IFactory<Pebble>'
        //                   to 'IFactory<object>'.
        //
        // Same reason this famous line doesn't compile either:
        //
        //     IEnumerable<int> ints = new List<int>();
        //     IEnumerable<object> objects = ints;      // ❌ CS0266
        //
        Console.WriteLine("  IFactory<Pebble> -> IFactory<object>   ❌  CS0266");
        Console.WriteLine();
        Console.WriteLine("  Variance needs an implicit REFERENCE conversion between type arguments.");
        Console.WriteLine("  int -> object is BOXING, not a reference conversion. Structs are excluded.");
        Console.WriteLine();
        Console.WriteLine("  ⇒ Building is a struct. So `out TReceptor` on INsiStructureMapper is not");
        Console.WriteLine("    merely unused by our code — it is INAPPLICABLE to the only receptor we");
        Console.WriteLine("    have. It blocks TryMap and buys nothing in return.");
        Console.WriteLine();
    }


    // =================================================================================
    //  DEMO 4 — Drop the `out`, get TryMap, get skip-and-collect
    // =================================================================================
    /// <summary>
    /// IFactoryFixed&lt;T&gt; is invariant, so TryMake is legal. That is the whole fix,
    /// and it unlocks exactly the importer behaviour we wanted.
    /// </summary>
    public static void Demo4_TheFix()
    {
        Header("DEMO 4 — the fix, and what it unlocks");

        // Stand-ins for a bounding box of NSI structures: even grams map, odd ones don't.
        int[] requested = [2, 3, 4, 5, 6];

        // --- today's behaviour: one bad record kills the whole import -----------------
        Console.WriteLine("  Fail-fast (what the importer does today):");
        try
        {
            List<Pebble> all = [];
            foreach (int grams in requested)
                all.Add(new PebbleFactory(grams).Make());     // throws on 3

            Console.WriteLine($"    got {all.Count} pebbles");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"    💥 {e.Message}");
            Console.WriteLine("    ...and every pebble already made is discarded.");
        }
        Console.WriteLine();

        // --- what TryMake enables: skip, and report what was skipped ------------------
        Console.WriteLine("  Skip-and-collect (what dropping `out` enables):");

        List<Pebble> made = [];
        List<int> rejected = [];

        foreach (int grams in requested)
        {
            IFactoryFixed<Pebble> factory = new PebbleFactory(grams);

            if (factory.TryMake(out Pebble pebble))
                made.Add(pebble);
            else
                rejected.Add(grams);        // ← the caller learns WHAT was dropped, not just how many
        }

        Console.WriteLine($"    made:     {string.Join(", ", made)}");
        Console.WriteLine($"    rejected: {string.Join(", ", rejected)}  (total {rejected.Sum()}g unaccounted for)");
        Console.WriteLine();
        Console.WriteLine("  That last number is the argument for returning the rejected structures");
        Console.WriteLine("  rather than a count: you can report how much exposure went missing.");
        Console.WriteLine();
    }


    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine($"  {title}");
        Console.WriteLine(new string('=', 78));
    }
}
