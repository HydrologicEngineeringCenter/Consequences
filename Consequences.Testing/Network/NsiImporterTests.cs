using Consequences.Network;

namespace Consequences.Testing.Network;

/// <summary>
/// The importer is a thin HTTP shell over <see cref="NsiJsonParser"/>; only the URL it
/// builds is testable without a live service. Response handling is covered by
/// <see cref="NsiJsonParserTests"/> and the projection by <see cref="BuildingMapperTests"/>,
/// so no test in this project contacts NSI.
/// </summary>
public class NsiImporterTests
{
    private const string BoundingBox =
        "-81.576,30.267,-81.573,30.267,-81.573,30.269,-81.576,30.269,-81.576,30.267";

    [Fact]
    public void StructuresEndpoint_BuildsTheFeatureCollectionUrl()
    {
        string url = NsiImporter.StructuresEndpoint(BoundingBox, "&fmt=fc");

        Assert.Equal(
            "https://nsi.sec.usace.army.mil/nsiapi/structures?bbox=" + BoundingBox + "&fmt=fc",
            url);
    }

    [Fact]
    public void StructuresEndpoint_BuildsTheFeatureStreamUrl()
    {
        string url = NsiImporter.StructuresEndpoint(BoundingBox, "&fmt=fs");

        Assert.EndsWith("&fmt=fs", url);
        Assert.Contains("bbox=" + BoundingBox, url);
    }
}
