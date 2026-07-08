using System.Globalization;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;

namespace OccTypeComparison;

/// <summary>
/// Reads the HEC-FIA/LifeSim "Occupancy Type Defaults" SQLite database.
/// Curves are stored as XML MonotonicUncertainCurve blobs in TEXT columns;
/// value/foundation-height uncertainty lives in Occupancy_Type_Variation_Lookup_Table.
/// </summary>
public static class SqliteReader
{
    public const string SourceName = "HEC-FIA SQLite";

    private static readonly (string Column, string Component)[] CurveColumns =
    [
        ("Structure_Damage_Curve", "structure"),
        ("Content_Damage_Curve", "contents"),
        ("Other_Damage_Curve", "other"),
        ("Vehicle_Damage_Curve", "vehicle"),
    ];

    public static List<CanonicalOccType> Read(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();

        Dictionary<string, Dictionary<string, string>> variations = ReadVariations(connection);

        var result = new List<CanonicalOccType>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Occupancy_Type_Lookup_Table ORDER BY Name";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string name = reader.GetString(reader.GetOrdinal("Name"));

            var curves = new List<CanonicalCurve>();
            foreach ((string column, string component) in CurveColumns)
            {
                CanonicalCurve? curve = ParseCurveXml(reader.GetString(reader.GetOrdinal(column)), component);
                if (curve is not null) curves.Add(curve);
            }

            var parameters = new Dictionary<string, string>
            {
                ["Compute_Structure_Damage"] = FormatBool(reader, "Compute_Structure_Damage"),
                ["Compute_Content_Damage"] = FormatBool(reader, "Compute_Content_Damage"),
                ["Compute_Other_Damage"] = FormatBool(reader, "Compute_Other_Damage"),
                ["Compute_Vehicle_Damage"] = FormatBool(reader, "Compute_Vehicle_Damage"),
                ["Price_Index"] = FormatDouble(reader, "Price_Index"),
                ["Reconstruction_Period"] = reader.GetInt32(reader.GetOrdinal("Reconstruction_Period")).ToString(CultureInfo.InvariantCulture),
                ["All_Warned"] = FormatBool(reader, "All_Warned"),
                ["All_Mobilized"] = FormatBool(reader, "All_Mobilized"),
                ["Fraction_In_Vehicles"] = FormatDouble(reader, "Fraction_In_Vehicles"),
                ["Vehicle_Occupancy_Rate"] = reader.GetInt32(reader.GetOrdinal("Vehicle_Occupancy_Rate")).ToString(CultureInfo.InvariantCulture),
                ["Fraction_Roof_Vs_Attic"] = FormatDouble(reader, "Fraction_Roof_Vs_Attic"),
                ["Fraction_Able_Vertical_Access"] = FormatDouble(reader, "Fraction_Able_Vertical_Access"),
            };

            if (variations.TryGetValue(name, out Dictionary<string, string>? typeVariations))
            {
                foreach ((string param, string value) in typeVariations)
                {
                    parameters[$"Variation:{param}"] = value;
                }
            }

            result.Add(new CanonicalOccType
            {
                Name = name,
                Source = SourceName,
                Description = reader.GetString(reader.GetOrdinal("Description")),
                DamageCategory = reader.GetString(reader.GetOrdinal("Damage_Category")),
                Curves = curves,
                Parameters = parameters,
            });
        }

        return result;
    }

    /// <summary>Parses a MonotonicUncertainCurve XML blob. Returns null for an empty &lt;Curve /&gt;.</summary>
    private static CanonicalCurve? ParseCurveXml(string xml, string component)
    {
        XElement root = XElement.Parse(xml);
        string distribution = root.Attribute("Distribution")?.Value ?? "None";
        var ordinates = root.Descendants("Ordinate").ToList();
        if (ordinates.Count == 0) return null;

        double[] xs = new double[ordinates.Count];
        double[] ys = new double[ordinates.Count];
        double[] sds = new double[ordinates.Count];
        bool hasUncertainty = false;

        for (int i = 0; i < ordinates.Count; i++)
        {
            xs[i] = double.Parse(ordinates[i].Attribute("X")!.Value, CultureInfo.InvariantCulture);
            ys[i] = double.Parse(ordinates[i].Attribute("Y")!.Value, CultureInfo.InvariantCulture);
            string? sdText = ordinates[i].Attribute("Normal_Standard_Deviation")?.Value;
            if (sdText is not null)
            {
                sds[i] = double.Parse(sdText, CultureInfo.InvariantCulture);
                hasUncertainty |= sds[i] > 0;
            }
        }

        double[]? mins = null, maxs = null;
        if (hasUncertainty)
        {
            mins = new double[ys.Length];
            maxs = new double[ys.Length];
            for (int i = 0; i < ys.Length; i++)
            {
                mins[i] = ys[i] - 2 * sds[i];
                maxs[i] = ys[i] + 2 * sds[i];
            }
        }

        return new CanonicalCurve
        {
            Component = component,
            Driver = "depth",
            Xs = xs,
            Ys = ys,
            YMins = mins,
            YMaxs = maxs,
            UncertaintyKind = hasUncertainty ? $"{distribution} (±2σ band)" : "None",
            CurveSource = "HEC-FIA defaults",
        };
    }

    private static Dictionary<string, Dictionary<string, string>> ReadVariations(SqliteConnection connection)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Distribution, Mean, Most_Likely, Minimum, Maximum, Standard_Deviation, A_Parameter, B_Parameter FROM Occupancy_Type_Variation_Lookup_Table";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string[] parts = reader.GetString(0).Split('>', 2);
            if (parts.Length != 2) continue;
            string distribution = reader.GetString(1);
            string formatted = distribution switch
            {
                "None" => "None",
                "Triangular" => $"Triangular(min={reader.GetDouble(4):0.###}, mostLikely={reader.GetDouble(3):0.###}, max={reader.GetDouble(5):0.###})",
                "Normal" => $"Normal(mean={reader.GetDouble(2):0.###}, sd={reader.GetDouble(6):0.###})",
                "Uniform" => $"Uniform(min={reader.GetDouble(4):0.###}, max={reader.GetDouble(5):0.###})",
                _ => $"{distribution}(mean={reader.GetDouble(2):0.###}, mostLikely={reader.GetDouble(3):0.###}, min={reader.GetDouble(4):0.###}, max={reader.GetDouble(5):0.###}, sd={reader.GetDouble(6):0.###}, a={reader.GetDouble(7):0.###}, b={reader.GetDouble(8):0.###})",
            };
            if (!result.TryGetValue(parts[0], out Dictionary<string, string>? map))
            {
                map = [];
                result[parts[0]] = map;
            }
            map[parts[1]] = formatted;
        }
        return result;
    }

    private static string FormatBool(SqliteDataReader reader, string column) =>
        reader.GetInt32(reader.GetOrdinal(column)) != 0 ? "true" : "false";

    private static string FormatDouble(SqliteDataReader reader, string column) =>
        reader.GetDouble(reader.GetOrdinal(column)).ToString("0.###", CultureInfo.InvariantCulture);
}
