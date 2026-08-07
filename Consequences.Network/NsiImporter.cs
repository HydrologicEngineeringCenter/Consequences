using System.Runtime.CompilerServices;
using System.Text;
using Consequences.Buildings;
using Consequences.Network.DTOs;
using Consequences.Network.Mapping;

namespace Consequences.Network;

/// <summary>
/// Fetches structures from the National Structure Inventory. This class owns the HTTP
/// concerns only — the response formats are handled by <see cref="NsiJsonParser"/> and
/// the projection onto a domain type by an <see cref="INsiStructureMapper{TReceptor}"/>.
///
/// The API root is resolved through <see cref="HecFwLink"/> on first use rather than being
/// compiled in, so NSI can move without a release of this library.
///
/// Request, parse and mapping failures all propagate. An empty result means the bounding
/// box held no structures, and nothing else.
/// </summary>
public static class NsiImporter
{
    private const string FEATURE_COLLECTION = "&fmt=fc";
    private const string FEATURE_STREAM = "&fmt=fs";

    private static readonly HttpClient _client = new();

    private static string? _root;


    /// <summary>
    /// Downloads the whole feature collection as <see cref="Building"/>s using the
    /// default occupancy types.
    /// </summary>
    public static Task<List<Building>> ProcessCollection(
        string boundingBox,
        CancellationToken cancellationToken = default) =>
        ProcessCollection(boundingBox, BuildingMapper.WithDefaultOccupancyTypes(), cancellationToken);


    /// <summary>
    /// Downloads the whole feature collection, projecting each structure with
    /// <paramref name="mapper"/>. Swap the mapper to import a different receptor type.
    /// </summary>
    public static async Task<List<TReceptor>> ProcessCollection<TReceptor>(
        string boundingBox,
        INsiStructureMapper<TReceptor> mapper,
        CancellationToken cancellationToken = default)
    {
        string root = await ResolveRoot(cancellationToken);
        string apiUrl = StructuresEndpoint(root, boundingBox, FEATURE_COLLECTION);

        using Stream jsonResponse = await _client.GetStreamAsync(apiUrl, cancellationToken);

        List<NsiStructure> structures =
            await NsiJsonParser.ParseFeatureCollectionAsync(jsonResponse, cancellationToken);

        return structures.Select(mapper.Map).ToList();
    }


    /// <summary>
    /// Streams the record-separated response as <see cref="Building"/>s using the
    /// default occupancy types.
    /// </summary>
    public static IAsyncEnumerable<Building> StreamCollection(
        string boundingBox,
        CancellationToken cancellationToken = default) =>
        StreamCollection(boundingBox, BuildingMapper.WithDefaultOccupancyTypes(), cancellationToken);


    /// <summary>
    /// Streams the record-separated response, projecting each structure as it arrives so
    /// the full collection never has to be held in memory.
    /// </summary>
    public static async IAsyncEnumerable<TReceptor> StreamCollection<TReceptor>(
        string boundingBox,
        INsiStructureMapper<TReceptor> mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string root = await ResolveRoot(cancellationToken);
        string apiUrl = StructuresEndpoint(root, boundingBox, FEATURE_STREAM);

        using Stream jsonResponse = await _client.GetStreamAsync(apiUrl, cancellationToken);
        using StreamReader reader = new(jsonResponse);

        await foreach (NsiStructure structure in
            NsiJsonParser.ParseFeatureStreamAsync(reader, cancellationToken))
        {
            yield return mapper.Map(structure);
        }
    }


    /// <summary>
    /// Resolved once per process. Two callers racing here cost a duplicate lookup and nothing
    /// else, since both arrive at the same address.
    /// </summary>
    private static async Task<string> ResolveRoot(CancellationToken cancellationToken)
    {
        if (_root != null)
            return _root;

        string root = await HecFwLink.ResolveAsync(KnownFwLinks.NsiApi, cancellationToken);

        // The endpoint is appended directly, and the registered target may or may not
        // already carry the separator.
        return _root = root.EndsWith('/') ? root : root + '/';
    }


    internal static string StructuresEndpoint(string root, string boundingBox, string directive)
    {
        StringBuilder url = new();

        url.Append(root);
        url.Append("structures?bbox=");
        url.Append(boundingBox);

        // directive to specify collection or stream
        url.Append(directive);

        return url.ToString();
    }
}
