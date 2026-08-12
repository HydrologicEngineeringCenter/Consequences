using System.Runtime.CompilerServices;
using Consequences.Buildings;
using Consequences.Network.DTOs;
using Consequences.Network.Mapping;

namespace Consequences.Network;

/// <summary>
/// Fetches structures from the National Structure Inventory. This class owns the HTTP
/// concerns only — the response formats are handled by <see cref="NsiJsonParser"/> and
/// the mapping onto a domain type by an <see cref="INsiStructureMapper{TReceptor}"/>.
///
/// The API root is resolved through <see cref="HecFwLink"/> rather than being compiled in,
/// so NSI can move without a release of this library.
///
/// Most callers want <see cref="Default"/>. Construct one to supply an <see cref="HttpClient"/>
/// — a stubbed handler in a test, or one carrying a proxy or credentials — or to pin the
/// importer to an API root of your own.
///
/// Request, parse and mapping failures all propagate. A request failure carries what the
/// service actually said, not just its status code. An empty result means the bounding box
/// held no structures, and nothing else.
/// </summary>
public sealed class NsiImporter
{
    private const string FEATURE_COLLECTION = "fc";
    private const string FEATURE_STREAM = "fs";

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
    public Task<List<Building>> GetBuildingsAsync(
        string boundingBox,
        CancellationToken cancellationToken = default) =>
        GetReceptorsAsync(boundingBox, BuildingMapper.WithDefaultOccupancyTypes(), cancellationToken);


    /// <summary>
    /// Downloads the whole feature collection, mapping each structure with
    /// <paramref name="mapper"/>. Swap the mapper to import a different receptor type.
    /// </summary>
    public async Task<List<TReceptor>> GetReceptorsAsync<TReceptor>(
        string boundingBox,
        INsiStructureMapper<TReceptor> mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boundingBox);
        ArgumentNullException.ThrowIfNull(mapper);

        string root = await ResolveRootAsync(cancellationToken);
        string apiUrl = StructuresEndpoint(root, boundingBox, FEATURE_COLLECTION);

        using HttpResponseMessage response = await SendAsync(apiUrl, cancellationToken);
        using Stream jsonResponse = await response.Content.ReadAsStreamAsync(cancellationToken);

        List<NsiStructure> structures =
            await NsiJsonParser.ParseFeatureCollectionAsync(jsonResponse, cancellationToken);

        return structures.Select(mapper.Map).ToList();
    }


    /// <summary>
    /// Streams the record-separated response as <see cref="Building"/>s using the
    /// default occupancy types.
    /// </summary>
    public IAsyncEnumerable<Building> StreamBuildingsAsync(
        string boundingBox,
        CancellationToken cancellationToken = default) =>
        StreamReceptorsAsync(boundingBox, BuildingMapper.WithDefaultOccupancyTypes(), cancellationToken);


    /// <summary>
    /// Streams the record-separated response, mapping each structure as it arrives so
    /// the full collection never has to be held in memory.
    /// </summary>
    /// <remarks>
    /// Split in two so the argument checks are not deferred: an iterator method runs no part of its
    /// body until the first <c>MoveNextAsync</c>, which would surface a bad bounding box or a null
    /// mapper at the caller's foreach rather than at the call itself.
    /// </remarks>
    public IAsyncEnumerable<TReceptor> StreamReceptorsAsync<TReceptor>(
        string boundingBox,
        INsiStructureMapper<TReceptor> mapper,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boundingBox);
        ArgumentNullException.ThrowIfNull(mapper);

        return StreamReceptorsCoreAsync(boundingBox, mapper, cancellationToken);
    }


    private async IAsyncEnumerable<TReceptor> StreamReceptorsCoreAsync<TReceptor>(
        string boundingBox,
        INsiStructureMapper<TReceptor> mapper,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string root = await ResolveRootAsync(cancellationToken);
        string apiUrl = StructuresEndpoint(root, boundingBox, FEATURE_STREAM);

        using HttpResponseMessage response = await SendAsync(apiUrl, cancellationToken);
        using Stream jsonResponse = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(jsonResponse);

        await foreach (NsiStructure structure in
            NsiJsonParser.ParseFeatureStreamAsync(reader, cancellationToken))
        {
            yield return mapper.Map(structure);
        }
    }


    /// <summary>
    /// Sends the request and hands back the response with its body unread, having first checked
    /// what the service said.
    ///
    /// <see cref="HttpClient.GetStreamAsync(string, CancellationToken)"/> would do the same fetch
    /// and then discard the response, so a failure arrives as a status code with no explanation.
    /// NSI puts the reason a request was rejected in the body, which is the part worth keeping.
    /// </summary>
    /// <remarks>
    /// The caller owns the returned response and disposes it. A send that throws disposes its own.
    /// <see cref="HttpCompletionOption.ResponseHeadersRead"/> is what
    /// <see cref="HttpClient.GetStreamAsync(string, CancellationToken)"/> uses internally, so the
    /// body still streams — nothing here waits for the whole response.
    /// </remarks>
    /// <exception cref="HttpRequestException">
    /// The service refused the request, or answered with something other than structures.
    /// </exception>
    private async Task<HttpResponseMessage> SendAsync(string apiUrl, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _client.GetAsync(
            apiUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                string detail = await ReadDetailAsync(response, cancellationToken);

                throw new HttpRequestException(
                    $"NSI returned {(int)response.StatusCode} {response.ReasonPhrase} for {apiUrl}." +
                    (detail.Length == 0 ? "" : $" Service said: {detail}"),
                    inner: null,
                    statusCode: response.StatusCode);
            }

            // An error page carries a 200 and would otherwise reach the parser as malformed JSON,
            // the same trap HecFwLink guards against on the resolve.
            if (response.Content.Headers.ContentType?.MediaType == "text/html")
            {
                throw new HttpRequestException(
                    $"NSI returned an HTML page rather than structures for {apiUrl}. " +
                    $"Service said: {await ReadDetailAsync(response, cancellationToken)}",
                    inner: null,
                    statusCode: response.StatusCode);
            }
        }
        catch
        {
            response.Dispose();
            throw;
        }

        return response;
    }


    /// <summary>
    /// The service's own words, for an exception message. An error body is small, and a truncated
    /// one still names the problem; a body that will not read is not worth failing twice over.
    /// </summary>
    private static async Task<string> ReadDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        const int LIMIT = 500;

        try
        {
            string body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

            return body.Length <= LIMIT ? body : body[..LIMIT] + "…";
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return "";
        }
    }


    /// <summary>
    /// Resolved once per importer. Two callers racing here cost a duplicate lookup and nothing
    /// else, since both arrive at the same address.
    /// </summary>
    private async Task<string> ResolveRootAsync(CancellationToken cancellationToken)
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


    /// <remarks>
    /// The box is escaped because an unescaped '#' in it would make a fragment of the rest of the
    /// URL, silently dropping &amp;fmt. That also encodes the separating commas as %2C, which NSI
    /// decodes to the same query it would have received unencoded.
    /// </remarks>
    internal static string StructuresEndpoint(string root, string boundingBox, string format) =>
        $"{root}structures?bbox={Uri.EscapeDataString(boundingBox)}&fmt={format}";


}
