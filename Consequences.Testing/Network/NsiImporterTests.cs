using System.Net;
using Consequences.Buildings;
using Consequences.Network;

namespace Consequences.Testing.Network;

/// <summary>
/// The importer is a thin HTTP shell over <see cref="NsiJsonParser"/>. Response handling is
/// covered by <see cref="NsiJsonParserTests"/> and the projection by
/// <see cref="BuildingMapperTests"/>; what is left here is the URL it builds and how it behaves
/// when the service answers badly. Every test fetches over a
/// <see cref="StubHttpMessageHandler"/>, so no test in this project contacts NSI.
/// </summary>
public class NsiImporterTests
{
    private const string Root = "https://nsi.sec.usace.army.mil/nsiapi/";

    private const string BoundingBox =
        "-81.576,30.267,-81.573,30.267,-81.573,30.269,-81.576,30.269,-81.576,30.267";

    /// <summary>
    /// <see cref="BoundingBox"/> as it goes on the wire. The separating commas are escaped because
    /// escaping is what stops a '#' from truncating the URL; NSI decodes them and answers exactly
    /// as it does for the unescaped form.
    /// </summary>
    private const string EncodedBoundingBox =
        "-81.576%2C30.267%2C-81.573%2C30.267%2C-81.573%2C30.269%2C-81.576%2C30.269%2C-81.576%2C30.267";

    [Fact]
    public void StructuresEndpoint_BuildsTheFeatureCollectionUrl()
    {
        string url = NsiImporter.StructuresEndpoint(Root, BoundingBox, "fc");

        Assert.Equal(
            Root + "structures?bbox=" + EncodedBoundingBox + "&fmt=fc",
            url);
    }

    [Fact]
    public void StructuresEndpoint_BuildsTheFeatureStreamUrl()
    {
        string url = NsiImporter.StructuresEndpoint(Root, BoundingBox, "fs");

        Assert.EndsWith("&fmt=fs", url);
        Assert.Contains("bbox=" + EncodedBoundingBox, url);
    }

    /// <summary>
    /// Left alone, a '#' makes a fragment of everything after it, so &amp;fmt never reaches NSI
    /// and the service answers in whichever format it defaults to.
    /// </summary>
    [Fact]
    public void StructuresEndpoint_EscapesABoxThatWouldOtherwiseTruncateTheUrl()
    {
        string url = NsiImporter.StructuresEndpoint(Root, "-81.576,30.267#", "fs");

        Assert.Equal(Root + "structures?bbox=-81.576%2C30.267%23&fmt=fs", url);
        Assert.EndsWith("&fmt=fs", new Uri(url).Query);
        Assert.Empty(new Uri(url).Fragment);
    }

    [Fact]
    public async Task ProcessCollection_MapsEveryFeatureInTheResponse()
    {
        NsiImporter importer = new(
            StubHttpMessageHandler.ClientReturning(NsiSamples.FeatureCollectionJson),
            Root);

        List<Building> buildings = await importer.GetBuildingsAsync(BoundingBox);

        Assert.Equal(2, buildings.Count);
    }

    [Fact]
    public async Task StreamCollection_MapsEveryRecordInTheResponse()
    {
        NsiImporter importer = new(
            StubHttpMessageHandler.ClientReturning(NsiSamples.FeatureStreamText),
            Root);

        List<Building> buildings = [];

        await foreach (Building building in importer.StreamBuildingsAsync(BoundingBox))
            buildings.Add(building);

        Assert.Equal(2, buildings.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ProcessCollection_PropagatesAServerError(HttpStatusCode status)
    {
        NsiImporter importer = new(
            StubHttpMessageHandler.ClientReturning("upstream said no", status),
            Root);

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(
            () => importer.GetBuildingsAsync(BoundingBox));

        Assert.Equal(status, error.StatusCode);
        Assert.Contains("upstream said no", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task StreamCollection_PropagatesAServerError(HttpStatusCode status)
    {
        NsiImporter importer = new(
            StubHttpMessageHandler.ClientReturning("upstream said no", status),
            Root);

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (Building _ in importer.StreamBuildingsAsync(BoundingBox)) { }
        });

        Assert.Equal(status, error.StatusCode);
        Assert.Contains("upstream said no", error.Message);
    }


    /// <summary>
    /// The failure the status code cannot report: a 200 carrying an error page. Left to the parser
    /// it surfaces as malformed JSON, which says nothing about what went wrong.
    /// </summary>
    [Fact]
    public async Task ProcessCollection_RejectsAnHtmlErrorPageCarryingA200()
    {
        HttpClient client = new(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Responding(
            HttpStatusCode.OK,
            "<!DOCTYPE html><html><body>Service unavailable</body></html>",
            "text/html")));

        NsiImporter importer = new(client, Root);

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(
            () => importer.GetBuildingsAsync(BoundingBox));

        Assert.Contains("HTML page", error.Message);
    }


    [Fact]
    public async Task StreamCollection_RejectsAnHtmlErrorPageCarryingA200()
    {
        HttpClient client = new(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Responding(
            HttpStatusCode.OK,
            "<!DOCTYPE html><html><body>Service unavailable</body></html>",
            "text/html")));

        NsiImporter importer = new(client, Root);

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (Building _ in importer.StreamBuildingsAsync(BoundingBox)) { }
        });

        Assert.Contains("HTML page", error.Message);
    }

    [Fact]
    public async Task AnApiRootWithoutATrailingSlashStillBuildsAUsableUrl()
    {
        StubHttpMessageHandler handler = new(_ =>
            StubHttpMessageHandler.Responding(HttpStatusCode.OK, NsiSamples.FeatureCollectionJson));

        NsiImporter importer = new(new HttpClient(handler), Root.TrimEnd('/'));

        await importer.GetBuildingsAsync(BoundingBox);

        Assert.Equal(
            Root + "structures?bbox=" + EncodedBoundingBox + "&fmt=fc",
            Assert.Single(handler.Requests).ToString());
    }

    [Fact]
    public async Task AnImporterWithoutAnApiRootResolvesTheForwardLinkFirst()
    {
        StubHttpMessageHandler handler = new(request =>
            request.RequestUri!.Host == "www.hec.usace.army.mil"
                ? StubHttpMessageHandler.Responding(HttpStatusCode.OK, Root + "\r\n")
                : StubHttpMessageHandler.Responding(HttpStatusCode.OK, NsiSamples.FeatureCollectionJson));

        NsiImporter importer = new(new HttpClient(handler));

        await importer.GetBuildingsAsync(BoundingBox);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("linkid=" + KnownFwLinks.NsiApi, handler.Requests[0].ToString());
        Assert.StartsWith(Root, handler.Requests[1].ToString());
    }

    [Fact]
    public async Task TheResolvedRootIsReusedAcrossCalls()
    {
        StubHttpMessageHandler handler = new(request =>
            request.RequestUri!.Host == "www.hec.usace.army.mil"
                ? StubHttpMessageHandler.Responding(HttpStatusCode.OK, Root + "\r\n")
                : StubHttpMessageHandler.Responding(HttpStatusCode.OK, NsiSamples.FeatureCollectionJson));

        NsiImporter importer = new(new HttpClient(handler));

        await importer.GetBuildingsAsync(BoundingBox);
        await importer.GetBuildingsAsync(BoundingBox);

        // One resolve, then a fetch per call.
        Assert.Equal(3, handler.Requests.Count);
        Assert.Single(handler.Requests, u => u.Host == "www.hec.usace.army.mil");
    }

    [Fact]
    public async Task AnUnregisteredForwardLinkFailsInsteadOfFetchingTheErrorPage()
    {
        NsiImporter importer = new(
            StubHttpMessageHandler.ClientReturning("<!DOCTYPE html><html>not found</html>"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => importer.GetBuildingsAsync(BoundingBox));
    }
}
