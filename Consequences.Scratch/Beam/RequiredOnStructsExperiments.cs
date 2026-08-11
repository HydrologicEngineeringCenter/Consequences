namespace Consequences.Scratch.Beam;

// =====================================================================================
//  IS `required` MOOT ON A STRUCT PROPERTY? SHOULD WE JUST REMOVE IT?
// =====================================================================================
//
//  The real type in Consequences Core:
//
//      public struct Building
//      {
//          public required OccupancyType OccupancyType { get; init; }   // reference type
//          ...
//      }
//
//  BuildingMapper.TryMap does `building = default;` on failure, which produces a Building
//  whose `required` non-nullable OccupancyType is NULL. So: is `required` doing anything
//  at all here, and would we be better off deleting it?
//
//  Short answer: NOT moot, and keep it.
//
//      `required` is a CONSTRUCTION-SITE guarantee, not a type invariant.
//
//  For a CLASS the two are the same thing — you cannot obtain an instance without going
//  through a constructor, so the guarantee holds everywhere.
//  For a STRUCT they come apart, because zeroed memory is a perfectly valid instance and
//  never passes through a constructor at all.
//
//  Demo1 shows what it catches. Demo2 shows the four ways around it. Demo3 shows why a
//  class doesn't have the problem. Demo4 is the argument for keeping it anyway.
//
// =====================================================================================


// ---------- simplistic stand-ins ------------------------------------------------------

/// <summary>Stand-in for OccupancyType — a reference type holding the thing we multiply by.</summary>
public class Curve
{
    public string Name { get; init; } = "unnamed";
    public float Factor { get; init; } = 1f;
    public override string ToString() => $"{Name} (x{Factor})";
}

/// <summary>
/// Stand-in for <c>Building</c>: a STRUCT with a required REFERENCE member.
/// </summary>
public struct Widget
{
    public required Curve Curve { get; init; }
    public float Value { get; init; }

    /// <summary>
    /// Mirrors Building.Compute — reaches straight through the required reference with no
    /// null check, because `required` says there is nothing to check.
    /// </summary>
    public readonly float Compute() => Value * Curve.Factor;
}

/// <summary>The same thing as a CLASS, for the Demo3 contrast.</summary>
public class WidgetClass
{
    public required Curve Curve { get; init; }
    public float Value { get; init; }
    public float Compute() => Value * Curve.Factor;
}

/// <summary>A class holding an unassigned struct field — one of the ways around `required`.</summary>
public class Holder
{
    public Widget Field;
}


public static class RequiredOnStructsExperiments
{
    public static void RunAll()
    {
        Demo1_WhatRequiredActuallyCatches();
        Demo2_TheFourWaysAroundIt();
        Demo3_OnAClassItIsARealInvariant();
        Demo4_WhyKeepItAnyway();
    }


    // =================================================================================
    //  DEMO 1 — `required` is enforced at every site you actually type
    // =================================================================================
    public static void Demo1_WhatRequiredActuallyCatches()
    {
        Header("DEMO 1 — what `required` does catch");

        // The happy path: set it, and everything works.
        Widget good = new() { Curve = new Curve { Name = "RES1", Factor = 0.2f }, Value = 210_000f };
        Console.WriteLine($"  new Widget {{ Curve = ..., Value = ... }}   ✅  Compute() = {good.Compute():N0}");

        // ❌ UNCOMMENT EITHER LINE — both are compile errors:
        //
        //     Widget missing = new() { Value = 210_000f };
        //     error CS9035: Required member 'Widget.Curve' must be set in the object
        //                   initializer or attribute constructor.
        //
        //     Widget empty = new Widget();
        //     error CS9035: (same)
        //
        // This is the real value of `required`, and it is not nothing: forgetting a member
        // in an object initializer is THE mistake it exists to catch, and it catches it.

        Console.WriteLine("  new Widget { Value = ... }                ❌  CS9035 — member omitted");
        Console.WriteLine("  new Widget()                              ❌  CS9035 — nothing set");
        Console.WriteLine();
    }


    // =================================================================================
    //  DEMO 2 — ...and defeated by anything that hands you zeroed memory
    // =================================================================================
    /// <summary>
    /// None of these go through a constructor, so there is no construction site for the
    /// compiler to check. All four produce a Widget with a NULL Curve despite Curve being
    /// declared non-nullable and required.
    /// </summary>
    public static void Demo2_TheFourWaysAroundIt()
    {
        Header("DEMO 2 — the four ways around it (all compile silently)");

        Widget[] slots = new Widget[3];

        Widget viaDefault = default;                                  // 1  ← what BuildingMapper does
        Widget viaArray = slots[0];                                   // 2
        Widget viaField = new Holder().Field;                         // 3
        Widget viaActivator = System.Activator.CreateInstance<Widget>();  // 4

        Report("default(Widget)", viaDefault);
        Report("new Widget[3] then [0]", viaArray);
        Report("new Holder().Field", viaField);
        Report("Activator.CreateInstance<Widget>()", viaActivator);

        Console.WriteLine();
        Console.WriteLine("  Note there is no compiler WARNING either. The nullable analyzer trusts");
        Console.WriteLine("  `required` and assumes Curve is non-null, so Compute() has no null check.");
        Console.WriteLine();

        // Watch it fail the way BuildingMapper's `building = default;` would if a caller
        // ignored the bool and used the value anyway.
        try
        {
            Console.WriteLine($"  viaDefault.Compute() -> {viaDefault.Compute()}");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("  viaDefault.Compute() -> 💥 NullReferenceException");
        }

        Console.WriteLine();

        static void Report(string how, Widget w) =>
            Console.WriteLine($"  {how,-38} Curve is null? {w.Curve is null}");
    }


    // =================================================================================
    //  DEMO 3 — why a class does not have this problem
    // =================================================================================
    public static void Demo3_OnAClassItIsARealInvariant()
    {
        Header("DEMO 3 — on a class, `required` IS a type invariant");

        WidgetClass good = new() { Curve = new Curve { Name = "COM1", Factor = 0.4f }, Value = 940_000f };
        Console.WriteLine($"  new WidgetClass {{ ... }}                  ✅  Compute() = {good.Compute():N0}");

        // There is no zeroed-memory path to a class instance. `default(WidgetClass)` is
        // not a WidgetClass-with-null-members — it is simply null, and null is VISIBLE:
        WidgetClass? none = default;
        Console.WriteLine($"  default(WidgetClass) is null?             {none is null}");
        Console.WriteLine();
        Console.WriteLine("  An array of a class type gives you nulls, not half-built instances:");

        WidgetClass?[] arr = new WidgetClass[2];
        Console.WriteLine($"  new WidgetClass[2] then [0] is null?      {arr[0] is null}");

        Console.WriteLine();
        Console.WriteLine("  So the failure is a null you can check for, at the point you got it —");
        Console.WriteLine("  instead of an object that looks valid and explodes later.");
        Console.WriteLine();
    }


    // =================================================================================
    //  DEMO 4 — so should we delete `required` from Building?
    // =================================================================================
    /// <summary>
    /// No. Removing it closes nothing and opens something.
    /// </summary>
    public static void Demo4_WhyKeepItAnyway()
    {
        Header("DEMO 4 — keep it");

        Console.WriteLine("  Removing `required` would change exactly two things:");
        Console.WriteLine();
        Console.WriteLine("    1. default(Widget) / arrays / fields — UNCHANGED. Still a null Curve.");
        Console.WriteLine("       Deleting the keyword closes none of Demo2's four holes.");
        Console.WriteLine();
        Console.WriteLine("    2. This would start compiling silently:");
        Console.WriteLine();
        Console.WriteLine("           Widget w = new() { Value = 210_000f };   // Curve forgotten");
        Console.WriteLine();
        Console.WriteLine("       ...and NRE later at Compute(), far from the mistake.");
        Console.WriteLine();
        Console.WriteLine("  That is trading real compile-time coverage for nothing. Keep it.");
        Console.WriteLine();
        Console.WriteLine("  The zeroing gap is not closable while Building stays a struct, and the");
        Console.WriteLine("  StructVsClassInvestigations benchmarks say struct was deliberate. So treat");
        Console.WriteLine("  'never let a zeroed Building escape' as API discipline, not a type promise.");
        Console.WriteLine("  That is precisely what the Try-pattern convention encodes: when TryMap");
        Console.WriteLine("  returns false, the out value is unspecified — do not read it.");
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
