namespace Consequences.Network;

/// <summary>
/// Resolves an HEC forward link to the address it currently points at. Endpoints are named by
/// a short, stable link id rather than a literal URL, so a service that moves is re-pointed on
/// the fwlink server instead of in a release of this library.
/// </summary>
/// <remarks>
/// Link ids are registered by HEC IT at https://bitbucket.hecdev.net/projects/IT/repos/fwlink/browse.
/// The "type=string" directive asks for the target as plain text instead of a redirect.
/// </remarks>
public static class HecFwLink
{
    private const string ROOT = "https://www.hec.usace.army.mil/fwlink/?linkid=";
    private const string AS_STRING = "&type=string";

    // A resolve that hangs must not hold up whatever is waiting on the endpoint.
    // PooledConnectionLifetime per the HttpClient guidelines:
    // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
    private static readonly HttpClient _shared = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    })
    {
        Timeout = TimeSpan.FromSeconds(15),
    };


    /// <summary>
    /// The forward link itself, for handing to a browser or to a client that should simply
    /// follow the redirect. Use <see cref="ResolveAsync"/> when the target is needed as text
    /// to build a longer URL from.
    /// </summary>
    public static string BuildUrl(string linkId) => ROOT + linkId;


    /// <summary>
    /// Asks the fwlink service what <paramref name="linkId"/> currently points at.
    /// </summary>
    /// <param name="linkId">The registered id to resolve.</param>
    /// <param name="client">
    /// The client to resolve with. Defaults to a shared one; pass your own to resolve over a
    /// stubbed handler in a test.
    /// </param>
    /// <param name="cancellationToken">Cancels the resolve.</param>
    /// <exception cref="InvalidOperationException">The link id is not registered.</exception>
    public static async Task<string> ResolveAsync(
        string linkId,
        HttpClient? client = null,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await (client ?? _shared).GetAsync(BuildUrl(linkId) + AS_STRING, cancellationToken);

        response.EnsureSuccessStatusCode();

        // The service terminates the target with a carriage return.
        string target = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        // An unregistered id comes back as a 200 carrying an HTML error page.
        if (target.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"fwlink id '{linkId}' did not resolve to an address.");

        return target;
    }
}


/// <summary>
/// The link ids this library resolves. Ids are registered with HEC IT, not chosen here.
/// </summary>
public static class KnownFwLinks
{
    /// <summary>
    /// Root of the National Structure Inventory API. NSI was an early adopter of fwlink,
    /// hence the very short id.
    /// </summary>
    public const string NsiApi = "1";
}
