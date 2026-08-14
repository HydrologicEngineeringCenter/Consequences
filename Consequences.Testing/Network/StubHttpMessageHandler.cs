using System.Net;
using System.Text;

namespace Consequences.Testing.Network;

/// <summary>
/// Answers requests from a delegate instead of the network. The handler is the seam an
/// <see cref="HttpClient"/> is built around, so injecting a client wrapping one of these puts
/// a test in charge of every status code and body the code under test sees.
/// </summary>
public sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    /// <summary>
    /// Every request this handler was asked for, in order, so a test can assert on the URL
    /// that was built and on how many times it was fetched.
    /// </summary>
    public List<Uri> Requests { get; } = [];

    /// <summary>
    /// Answers every request with <paramref name="body"/>.
    /// </summary>
    public static HttpClient ClientReturning(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new StubHttpMessageHandler(_ => Responding(status, body)));

    public static HttpResponseMessage Responding(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body) };

    /// <summary>
    /// Answers with a body of <paramref name="mediaType"/>, for the cases where the importer
    /// decides on the content type rather than the status.
    /// </summary>
    public static HttpResponseMessage Responding(HttpStatusCode status, string body, string mediaType) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);

        return Task.FromResult(respond(request));
    }
}
