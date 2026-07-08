namespace OccTypeComparison;

public enum CurveVerdict
{
    Identical,      // max abs diff < 0.5 damage points
    Minor,          // max abs diff < 5 damage points
    Divergent,      // max abs diff >= 5 damage points
    MissingInFia,
    MissingInGo,
}

public record CurveComparison
{
    public required string Component { get; init; }
    public CanonicalCurve? Fia { get; init; }
    public CanonicalCurve? Go { get; init; }
    public double MaxAbsDiff { get; init; }
    public double MeanAbsDiff { get; init; }
    public required CurveVerdict Verdict { get; init; }
}

public record OccTypeComparisonResult
{
    public required string Name { get; init; }
    public CanonicalOccType? Fia { get; init; }
    public CanonicalOccType? Go { get; init; }
    public List<CurveComparison> CurveComparisons { get; init; } = [];
    /// <summary>Worst curve divergence, used for report ordering.</summary>
    public double Severity => CurveComparisons.Count == 0 ? 0 : CurveComparisons.Max(c => c.MaxAbsDiff);
}

public record InventoryRow(string Kind, string Item, int FiaCount, int GoCount);

public static class Comparator
{
    /// <summary>Both sources compare on depth-driven damage; go-consequences falls back to "default" when "depth" is absent.</summary>
    private static readonly string[] ComparableComponents = ["structure", "contents"];

    public static List<OccTypeComparisonResult> Compare(List<CanonicalOccType> fiaTypes, List<CanonicalOccType> goTypes)
    {
        Dictionary<string, CanonicalOccType> fiaByName = fiaTypes.ToDictionary(t => t.Name);
        Dictionary<string, CanonicalOccType> goByName = goTypes.ToDictionary(t => t.Name);

        var results = new List<OccTypeComparisonResult>();
        foreach (string name in fiaByName.Keys.Union(goByName.Keys).OrderBy(n => n, StringComparer.Ordinal))
        {
            fiaByName.TryGetValue(name, out CanonicalOccType? fia);
            goByName.TryGetValue(name, out CanonicalOccType? go);

            var curveComparisons = new List<CurveComparison>();
            if (fia is not null && go is not null)
            {
                foreach (string component in ComparableComponents)
                {
                    CanonicalCurve? fiaCurve = DepthCurve(fia, component);
                    CanonicalCurve? goCurve = DepthCurve(go, component);
                    curveComparisons.Add(CompareCurves(component, fiaCurve, goCurve));
                }
            }

            results.Add(new OccTypeComparisonResult
            {
                Name = name,
                Fia = fia,
                Go = go,
                CurveComparisons = curveComparisons,
            });
        }

        return results;
    }

    public static CanonicalCurve? DepthCurve(CanonicalOccType occType, string component) =>
        occType.Curves.FirstOrDefault(c => c.Component == component && c.Driver == "depth")
        ?? occType.Curves.FirstOrDefault(c => c.Component == component && c.Driver == "default");

    private static CurveComparison CompareCurves(string component, CanonicalCurve? fia, CanonicalCurve? go)
    {
        if (fia is null || go is null)
        {
            return new CurveComparison
            {
                Component = component,
                Fia = fia,
                Go = go,
                Verdict = fia is null ? CurveVerdict.MissingInFia : CurveVerdict.MissingInGo,
            };
        }

        // Evaluate both on the union depth grid; outside a curve's range its endpoint value
        // extends flat (the standard depth-damage convention: 0 below, terminal damage above).
        double[] grid = [.. fia.Xs.Concat(go.Xs).Distinct().OrderBy(x => x)];
        double maxDiff = 0, sumDiff = 0;
        foreach (double x in grid)
        {
            double diff = Math.Abs(Interpolate(fia, x) - Interpolate(go, x));
            maxDiff = Math.Max(maxDiff, diff);
            sumDiff += diff;
        }
        double meanDiff = sumDiff / grid.Length;

        return new CurveComparison
        {
            Component = component,
            Fia = fia,
            Go = go,
            MaxAbsDiff = maxDiff,
            MeanAbsDiff = meanDiff,
            Verdict = maxDiff < 0.5 ? CurveVerdict.Identical
                    : maxDiff < 5.0 ? CurveVerdict.Minor
                    : CurveVerdict.Divergent,
        };
    }

    public static double Interpolate(CanonicalCurve curve, double x)
    {
        double[] xs = curve.Xs, ys = curve.Ys;
        if (x <= xs[0]) return ys[0];
        if (x >= xs[^1]) return ys[^1];
        int index = Array.BinarySearch(xs, x);
        if (index >= 0) return ys[index];
        index = ~index;
        double fraction = (x - xs[index - 1]) / (xs[index] - xs[index - 1]);
        return ys[index - 1] + fraction * (ys[index] - ys[index - 1]);
    }

    /// <summary>
    /// The "each source has what the other lacks" catalog: every curve family and parameter,
    /// with how many occupancy types carry it in each source.
    /// </summary>
    public static List<InventoryRow> BuildInventory(List<CanonicalOccType> fiaTypes, List<CanonicalOccType> goTypes)
    {
        var rows = new List<InventoryRow>();

        var curveKeys = fiaTypes.Concat(goTypes)
            .SelectMany(t => t.Curves.Select(c => $"{c.Component} × {c.Driver}"))
            .Distinct()
            .OrderBy(k => k, StringComparer.Ordinal);
        foreach (string key in curveKeys)
        {
            rows.Add(new InventoryRow("Damage function", key,
                fiaTypes.Count(t => t.Curves.Any(c => $"{c.Component} × {c.Driver}" == key)),
                goTypes.Count(t => t.Curves.Any(c => $"{c.Component} × {c.Driver}" == key))));
        }

        var parameterKeys = fiaTypes.Concat(goTypes)
            .SelectMany(t => t.Parameters.Keys)
            .Distinct()
            .OrderBy(k => k, StringComparer.Ordinal);
        foreach (string key in parameterKeys)
        {
            rows.Add(new InventoryRow("Parameter", key,
                fiaTypes.Count(t => t.Parameters.ContainsKey(key)),
                goTypes.Count(t => t.Parameters.ContainsKey(key))));
        }

        rows.Add(new InventoryRow("Metadata", "Description",
            fiaTypes.Count(t => !string.IsNullOrEmpty(t.Description)),
            goTypes.Count(t => !string.IsNullOrEmpty(t.Description))));
        rows.Add(new InventoryRow("Metadata", "Damage category",
            fiaTypes.Count(t => !string.IsNullOrEmpty(t.DamageCategory)),
            goTypes.Count(t => !string.IsNullOrEmpty(t.DamageCategory))));
        rows.Add(new InventoryRow("Metadata", "Curve source attribution",
            fiaTypes.Count(t => t.Curves.Any(c => c.CurveSource is not null and not "HEC-FIA defaults")),
            goTypes.Count(t => t.Curves.Any(c => !string.IsNullOrEmpty(c.CurveSource)))));
        rows.Add(new InventoryRow("Metadata", "Per-ordinate uncertainty",
            fiaTypes.Count(t => t.Curves.Any(c => c.UncertaintyKind != "None")),
            goTypes.Count(t => t.Curves.Any(c => c.UncertaintyKind != "None"))));

        return rows;
    }
}
