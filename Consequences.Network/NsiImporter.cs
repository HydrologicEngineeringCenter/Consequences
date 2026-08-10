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
/// The API root is resolved through <see cref="HecFwLink"/> rather than being compiled in,
/// so NSI can move without a release of this library.
///
/// Most callers want <see cref="Default"/>. Construct one to supply an <see cref="HttpClient"/>
/// — a stubbed handler in a test, or one carrying a proxy or credentials — or to pin the
/// importer to an API root of your own.
///
/// Request, parse and mapping failures all propagate. An empty result means the bounding
/// box held no structures, and nothing else.
/// </summary>
public sealed class NsiImporter
{
    private const string FEATURE_COLLECTION = "&fmt=fc";
    private const string FEATURE_STREAM = "&fmt=fs";

    // PooledConnectionLifetime per the HttpClient guidelines: a client this long-lived would
    // otherwise hold connections that never notice DNS moving underneath them.
    // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
    private static readonly HttpClient _shared = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    });

    private readonly HttpClient _client;
    private readonly string? _fixedRoot;

    private string? _root;


    /// <param name="client">
    /// The client to fetch with, used for the <see cref="HecFwLink"/> resolve as well. Defaults
    /// to a shared one; pass your own to fetch over a stubbed handler in a test.
    /// </param>
    /// <param name="apiRoot">
    /// The API root to fetch from. Defaults to resolving <see cref="KnownFwLinks.NsiApi"/>; pass
    /// one to pin the importer to a fixed address and skip the resolve entirely.
    /// </param>
    public NsiImporter(HttpClient? client = null, string? apiRoot = null)
    {
        _client = client ?? _shared;
        _fixedRoot = apiRoot == null ? null : WithTrailingSlash(apiRoot);
    }


    /// <summary>
    /// Shared importer against the public NSI service.
    /// </summary>
    public static NsiImporter Default { get; } = new();


    /// <summary>
    /// Downloads the whole feature collection as <see cref="Building"/>s using the
    /// default occupancy types.
    /// </summary>
    public Task<List<Building>> ProcessCollection(
        string boundingBox,
        CancellationToken cancellationToken = default) =>
        ProcessCollection(boundingBox, BuildingMapper.WithDefaultOccupancyTypes(), cancellationToken);


    /// <summary>
    /// Downloads the whole feature collection, projecting each structure with
    /// <paramref name="mapper"/>. Swap the mapper to import a different receptor type.
    /// </summary>
    public async Task<List<TReceptor>> ProcessCollection<TReceptor>(
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
    public IAsyncEnumerable<Building> StreamCollection(
        string boundingBox,
        CancellationToken cancellationToken = default) =>
        StreamCollection(boundingBox, BuildingMapper.WithDefaultOccupancyTypes(), cancellationToken);


    /// <summary>
    /// Streams the record-separated response, projecting each structure as it arrives so
    /// the full collection never has to be held in memory.
    /// </summary>
    public async IAsyncEnumerable<TReceptor> StreamCollection<TReceptor>(
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
    /// Resolved once per importer. Two callers racing here cost a duplicate lookup and nothing
    /// else, since both arrive at the same address.
    /// </summary>
    private async Task<string> ResolveRoot(CancellationToken cancellationToken)
    {
        if (_fixedRoot != null)
            return _fixedRoot;

        if (_root != null)
            return _root;

        string root = await HecFwLink.ResolveAsync(KnownFwLinks.NsiApi, _client, cancellationToken);

        return _root = WithTrailingSlash(root);
    }


    /// <summary>
    /// The endpoint is appended directly, and a root may or may not already carry the separator.
    /// </summary>
    private static string WithTrailingSlash(string root) =>
        root.EndsWith('/') ? root : root + '/';


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
