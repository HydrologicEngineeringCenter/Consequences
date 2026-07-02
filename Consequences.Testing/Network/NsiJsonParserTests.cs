using Consequences.Network;
using Consequences.Network.DTOs;

namespace Consequences.Testing.Network;

/// <summary>
/// Format-level tests. Everything here runs against in-memory samples, so the NSI
/// service is never contacted.
/// </summary>
public class NsiJsonParserTests
{
    [Fact]
    public void ParseFeatureCollection_ReadsEveryFeature()
    {
        List<NsiStructure> structures =
            NsiJsonParser.ParseFeatureCollection(NsiSamples.FeatureCollectionJson);

        Assert.Equal(2, structures.Count);
        Assert.Equal([492269438L, 492269439L], structures.Select(s => s.FdId));
        Assert.Equal(["RES1-1SNB", "COM1"], structures.Select(s => s.Occtype));
    }

    [Fact]
    public void ParseFeatureCollection_ReadsQuotedNumbers()
    {
        // fd_id arrives quoted for the first feature and bare for the second.
        List<NsiStructure> structures =
            NsiJsonParser.ParseFeatureCollection(NsiSamples.FeatureCollectionJson);

        Assert.Equal(492269438L, structures[0].FdId);
        Assert.Equal(1.5f, structures[0].FoundHt);
        Assert.Equal(210000.0, structures[0].ValStruct);
    }

    [Fact]
    public void ParseFeatureCollection_ReturnsEmptyForJsonNull()
    {
        Assert.Empty(NsiJsonParser.ParseFeatureCollection("null"));
    }

    [Fact]
    public async Task ParseFeatureStreamAsync_YieldsSameStructuresAsFeatureCollection()
    {
        List<NsiStructure> collected =
            NsiJsonParser.ParseFeatureCollection(NsiSamples.FeatureCollectionJson);

        using StringReader reader = new(NsiSamples.FeatureStreamText);

        List<NsiStructure> streamed = [];
        await foreach (NsiStructure s in NsiJsonParser.ParseFeatureStreamAsync(reader))
            streamed.Add(s);

        Assert.Equal(collected.Select(s => s.FdId), streamed.Select(s => s.FdId));
        Assert.Equal(collected.Select(s => s.Occtype), streamed.Select(s => s.Occtype));
    }

    [Fact]
    public async Task ParseFeatureStreamAsync_SkipsBlankRecords()
    {
        string withBlanks =
            NsiSamples.RecordSeparator + Environment.NewLine +
            Environment.NewLine +
            NsiSamples.FeatureStreamText;

        using StringReader reader = new(withBlanks);

        int count = 0;
        await foreach (NsiStructure _ in NsiJsonParser.ParseFeatureStreamAsync(reader))
            count++;

        Assert.Equal(2, count);
    }

    [Fact]
    public void TryParseFeature_AcceptsRecordsWithAndWithoutTheSeparator()
    {
        string record = NsiSamples.FeatureStreamRecords[0];

        Assert.True(NsiJsonParser.TryParseFeature(record, out NsiStructure? bare));
        Assert.True(NsiJsonParser.TryParseFeature(NsiSamples.RecordSeparator + record, out NsiStructure? prefixed));

        Assert.Equal(492269438L, bare!.FdId);
        Assert.Equal(bare, prefixed);
    }

    [Fact]
    public void TryParseFeature_RejectsFeaturesWithoutProperties()
    {
        const string geometryOnly =
            """{"type":"Feature","geometry":{"type":"Point","coordinates":[-81.5,30.2]}}""";

        Assert.False(NsiJsonParser.TryParseFeature(geometryOnly, out NsiStructure? structure));
        Assert.Null(structure);
    }
}
