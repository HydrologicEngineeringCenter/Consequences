using System.Text.Json;

namespace OccTypeComparison;

/// <summary>
/// Reads the go-consequences structures/occtypes.json format: occupancy types with
/// component damage functions keyed by damage driver, each ordinate an uncertainty distribution.
/// </summary>
public static class GoJsonReader
{
    public const string SourceName = "go-consequences";

    public static List<CanonicalOccType> Read(string jsonPath)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        JsonElement occTypes = document.RootElement.GetProperty("occupancytypes");

        var result = new List<CanonicalOccType>();
        foreach (JsonProperty occTypeProperty in occTypes.EnumerateObject())
        {
            JsonElement occType = occTypeProperty.Value;
            string name = occType.GetProperty("name").GetString() ?? occTypeProperty.Name;

            var curves = new List<CanonicalCurve>();
            if (occType.TryGetProperty("componentdamagefunctions", out JsonElement components))
            {
                foreach (JsonProperty component in components.EnumerateObject())
                {
                    if (!component.Value.TryGetProperty("damagefunctions", out JsonElement damageFunctions)) continue;
                    foreach (JsonProperty driver in damageFunctions.EnumerateObject())
                    {
                        curves.Add(ParseDamageFunction(component.Name, driver.Name, driver.Value));
                    }
                }
            }

            result.Add(new CanonicalOccType
            {
                Name = name,
                Source = SourceName,
                Curves = curves,
            });
        }

        return [.. result.OrderBy(t => t.Name, StringComparer.Ordinal)];
    }

    private static CanonicalCurve ParseDamageFunction(string component, string driverKey, JsonElement element)
    {
        string? source = element.TryGetProperty("source", out JsonElement s) ? s.GetString() : null;
        JsonElement function = element.GetProperty("damagefunction");

        double[] xs = [.. function.GetProperty("xvalues").EnumerateArray().Select(x => x.GetDouble())];
        JsonElement[] distributions = [.. function.GetProperty("ydistributions").EnumerateArray()];

        double[] ys = new double[distributions.Length];
        double[] mins = new double[distributions.Length];
        double[] maxs = new double[distributions.Length];
        bool hasBand = false;
        var kinds = new SortedSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < distributions.Length; i++)
        {
            (ys[i], mins[i], maxs[i], string kind) = ParseDistribution(distributions[i]);
            if (kind != "Deterministic") kinds.Add(kind);
            hasBand |= maxs[i] > mins[i];
        }

        return new CanonicalCurve
        {
            Component = component,
            Driver = driverKey,
            Xs = xs,
            Ys = ys,
            YMins = hasBand ? mins : null,
            YMaxs = hasBand ? maxs : null,
            UncertaintyKind = kinds.Count == 0 ? "None" : string.Join(", ", kinds),
            CurveSource = source,
        };
    }

    /// <summary>Central value plus a min/max band per distribution type (Normal band is ±2σ).</summary>
    private static (double Central, double Min, double Max, string Kind) ParseDistribution(JsonElement distribution)
    {
        string type = distribution.GetProperty("type").GetString() ?? "unknown";
        JsonElement p = distribution.GetProperty("parameters");
        switch (type)
        {
            case "DeterministicDistribution":
            {
                double value = p.GetProperty("value").GetDouble();
                return (value, value, value, "Deterministic");
            }
            case "TriangularDistribution":
            {
                double mostLikely = p.GetProperty("mostlikely").GetDouble();
                return (mostLikely, p.GetProperty("min").GetDouble(), p.GetProperty("max").GetDouble(), "Triangular");
            }
            case "NormalDistribution":
            {
                double mean = p.GetProperty("mean").GetDouble();
                double sd = p.GetProperty("standarddeviation").GetDouble();
                return (mean, mean - 2 * sd, mean + 2 * sd, "Normal");
            }
            case "EmpiricalDistribution":
            {
                double[] binStarts = [.. p.GetProperty("binstarts").EnumerateArray().Select(x => x.GetDouble())];
                double[] binCounts = [.. p.GetProperty("bincounts").EnumerateArray().Select(x => x.GetDouble())];
                double binWidth = p.GetProperty("binwidth").GetDouble();
                double total = binCounts.Sum();
                double mean = total > 0
                    ? binStarts.Zip(binCounts, (start, count) => (start + binWidth / 2) * count).Sum() / total
                    : 0;
                double min = p.TryGetProperty("minvalue", out JsonElement mn) ? mn.GetDouble() : binStarts.Min();
                double max = p.TryGetProperty("maxvalue", out JsonElement mx) ? mx.GetDouble() : binStarts.Max() + binWidth;
                return (mean, min, max, "Empirical");
            }
            default:
                throw new NotSupportedException($"Unknown distribution type '{type}'.");
        }
    }
}
