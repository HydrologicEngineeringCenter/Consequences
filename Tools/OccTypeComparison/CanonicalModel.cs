namespace OccTypeComparison;

/// <summary>
/// A single damage function in the neutral intermediate format both sources are mapped onto.
/// Y values are central estimates; YMins/YMaxs carry an uncertainty band when the source has one.
/// </summary>
public record CanonicalCurve
{
    public required string Component { get; init; }   // structure | contents | other | vehicle
    public required string Driver { get; init; }      // depth | erosion | default | composite ("depth, salinity", ...)
    public required double[] Xs { get; init; }
    public required double[] Ys { get; init; }
    public double[]? YMins { get; init; }
    public double[]? YMaxs { get; init; }
    public string UncertaintyKind { get; init; } = "None";
    public string? CurveSource { get; init; }
}

public record CanonicalOccType
{
    public required string Name { get; init; }
    public required string Source { get; init; }      // "HEC-FIA SQLite" | "go-consequences"
    public string? Description { get; init; }
    public string? DamageCategory { get; init; }
    public List<CanonicalCurve> Curves { get; init; } = [];
    // Everything that isn't a curve, flattened to strings (life-loss params, variation-table entries, ...).
    public Dictionary<string, string> Parameters { get; init; } = [];
}
