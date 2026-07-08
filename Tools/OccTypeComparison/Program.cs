using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using OccTypeComparison;

// Defaults; override with --sqlite <path> --json <path> --out <dir>
string sqlitePath = @"E:\Occupancy Type Defaults.sqlite";
string jsonPath = Path.Combine(AppContext.BaseDirectory, "data", "occtypes.json");
string outputDir = Path.Combine(FindProjectDir(), "output");

for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--sqlite": sqlitePath = args[++i]; break;
        case "--json": jsonPath = args[++i]; break;
        case "--out": outputDir = args[++i]; break;
    }
}

if (!File.Exists(sqlitePath))
{
    Console.Error.WriteLine($"SQLite database not found: {sqlitePath} (pass --sqlite <path>)");
    return 1;
}
if (!File.Exists(jsonPath))
{
    Console.Error.WriteLine($"occtypes.json not found: {jsonPath} (pass --json <path>)");
    return 1;
}
Directory.CreateDirectory(outputDir);

List<CanonicalOccType> fiaTypes = SqliteReader.Read(sqlitePath);
List<CanonicalOccType> goTypes = GoJsonReader.Read(jsonPath);
Console.WriteLine($"Read {fiaTypes.Count} occupancy types from HEC-FIA SQLite, {goTypes.Count} from go-consequences JSON.");

var serializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};
string fiaCanonicalPath = Path.Combine(outputDir, "occtypes-fia.canonical.json");
string goCanonicalPath = Path.Combine(outputDir, "occtypes-go.canonical.json");
File.WriteAllText(fiaCanonicalPath, JsonSerializer.Serialize(fiaTypes, serializerOptions));
File.WriteAllText(goCanonicalPath, JsonSerializer.Serialize(goTypes, serializerOptions));

List<OccTypeComparisonResult> results = Comparator.Compare(fiaTypes, goTypes);
List<InventoryRow> inventory = Comparator.BuildInventory(fiaTypes, goTypes);

var provenance = new ReportProvenance(
    SqlitePath: sqlitePath,
    JsonUrl: "https://github.com/USACE/go-consequences/blob/main/structures/occtypes.json",
    JsonCommit: "dff647c (2021-12-27)",
    GeneratedOn: DateTime.Now.ToString("yyyy-MM-dd"));

string reportPath = Path.Combine(outputDir, "comparison-report.html");
File.WriteAllText(reportPath, HtmlReport.Render(results, inventory, provenance));

int shared = results.Count(r => r.Fia is not null && r.Go is not null);
int divergentCurves = results.SelectMany(r => r.CurveComparisons).Count(c => c.Verdict == CurveVerdict.Divergent);
Console.WriteLine($"Shared types: {shared} · FIA-only: {results.Count(r => r.Go is null)} · go-only: {results.Count(r => r.Fia is null)}");
Console.WriteLine($"Divergent depth curves (max diff >= 5 pts): {divergentCurves}");
Console.WriteLine();
Console.WriteLine($"Wrote {fiaCanonicalPath}");
Console.WriteLine($"Wrote {goCanonicalPath}");
Console.WriteLine($"Wrote {reportPath}");
return 0;

// Output lands in Tools/OccTypeComparison/output/ regardless of build configuration.
static string FindProjectDir()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "OccTypeComparison.csproj")))
        dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
