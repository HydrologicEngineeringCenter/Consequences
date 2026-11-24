using System.Xml;
using System.Xml.Linq;
using Consequences.Occupancy;
using Numerics.Data;

namespace Consequences.Testing.Serialization;

public class OccupancyTypeSerializationTests
{
    private const string MinimalXml = """
        <OccupancyType Name="Example">
          <StructureDamageFunction StrictX="true" StrictY="false" OrderX="Ascending" OrderY="Ascending">
            <Ordinates><Ordinate X="0" Y="0" /><Ordinate X="1" Y="1" /></Ordinates>
          </StructureDamageFunction>
          <ContentDamageFunction StrictX="true" StrictY="false" OrderX="Ascending" OrderY="Ascending">
            <Ordinates><Ordinate X="0" Y="0" /><Ordinate X="1" Y="1" /></Ordinates>
          </ContentDamageFunction>
        </OccupancyType>
        """;

    [Fact]
    public void Read_PreservesOptionalDefaults()
    {
        OccupancyType occupancy = OccupancyType.FromXElement(XElement.Parse(MinimalXml));

        Assert.Equal("Example", occupancy.Name);
        Assert.Equal(1f, occupancy.StructureValuePercentageOfTheMean);
        Assert.Equal(1f, occupancy.ContentValuePercentageOfTheMean);
        Assert.Equal((byte)2, occupancy.VehicleOccupancyRate);
        Assert.Equal(0f, occupancy.FoundationHeightOffset);
        Assert.Equal(0f, occupancy.FromFloorDepthThreshold);
        Assert.Equal(0f, occupancy.FromCeilingDepthThreshold);
        Assert.False(occupancy.CollectivelyWarned);
        Assert.False(occupancy.CollectivelyMobilize);
    }

    [Fact]
    public void Read_RequiresName()
    {
        XElement xml = XElement.Parse(MinimalXml);
        xml.Attribute("Name")!.Remove();

        Assert.Throws<XmlException>(() => OccupancyType.FromXElement(xml));
    }

    [Theory]
    [InlineData("StructureDamageFunction")]
    [InlineData("ContentDamageFunction")]
    public void Read_RequiresCompleteCurve(string curveName)
    {
        foreach (string attribute in new[] { "StrictX", "StrictY", "OrderX", "OrderY" })
        {
            XElement xml = XElement.Parse(MinimalXml);
            xml.Element(curveName)!.Attribute(attribute)!.Remove();
            Assert.Throws<XmlException>(() => OccupancyType.FromXElement(xml));
        }

        foreach (string attribute in new[] { "X", "Y" })
        {
            XElement xml = XElement.Parse(MinimalXml);
            xml.Element(curveName)!.Element("Ordinates")!.Elements().Last().Attribute(attribute)!.Remove();
            Assert.Throws<XmlException>(() => OccupancyType.FromXElement(xml));
        }

        XElement missingOrdinates = XElement.Parse(MinimalXml);
        missingOrdinates.Element(curveName)!.Element("Ordinates")!.Remove();
        Assert.Throws<XmlException>(() => OccupancyType.FromXElement(missingOrdinates));

        XElement missingCurve = XElement.Parse(MinimalXml);
        missingCurve.Element(curveName)!.Remove();
        Assert.Throws<XmlException>(() => OccupancyType.FromXElement(missingCurve));
    }

    [Fact]
    public void RoundTrip_RestoresOccupancyAndDamageFunctions()
    {
        var original = new OccupancyType
        {
            Name = "Example",
            StructureDamageFunction = new OrderedPairedData(
                [-1, 2], [0.25, 0.75],
                strictOnX: true, SortOrder.Ascending,
                strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                [-2, 4], [0.875, 0.125],
                strictOnX: false, SortOrder.Ascending,
                strictOnY: true, SortOrder.Descending),
            FoundationHeightOffset = 0.5f,
            StructureValuePercentageOfTheMean = 1.25f,
            ContentValuePercentageOfTheMean = 0.75f,
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
            VehicleOccupancyRate = 4,
            FromFloorDepthThreshold = 2.5f,
            FromCeilingDepthThreshold = 3.5f
        };

        string xml = original.SaveToXElement().ToString();
        OccupancyType restored = OccupancyType.FromXElement(XElement.Parse(xml));

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.FoundationHeightOffset, restored.FoundationHeightOffset);
        Assert.Equal(original.StructureValuePercentageOfTheMean, restored.StructureValuePercentageOfTheMean);
        Assert.Equal(original.ContentValuePercentageOfTheMean, restored.ContentValuePercentageOfTheMean);
        Assert.Equal(original.CollectivelyWarned, restored.CollectivelyWarned);
        Assert.Equal(original.CollectivelyMobilize, restored.CollectivelyMobilize);
        Assert.Equal(original.VehicleOccupancyRate, restored.VehicleOccupancyRate);
        Assert.Equal(original.FromFloorDepthThreshold, restored.FromFloorDepthThreshold);
        Assert.Equal(original.FromCeilingDepthThreshold, restored.FromCeilingDepthThreshold);
        AssertCurve(original.StructureDamageFunction, restored.StructureDamageFunction);
        AssertCurve(original.ContentDamageFunction, restored.ContentDamageFunction);
    }

    private static void AssertCurve(OrderedPairedData expected, OrderedPairedData actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.StrictX, actual.StrictX);
        Assert.Equal(expected.StrictY, actual.StrictY);
        Assert.Equal(expected.OrderX, actual.OrderX);
        Assert.Equal(expected.OrderY, actual.OrderY);
        Assert.Equal(expected.Count, actual.Count);
        Assert.True(actual.IsValid);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].X, actual[i].X);
            Assert.Equal(expected[i].Y, actual[i].Y);
        }

        double midpoint = (expected[0].X + expected[1].X) / 2;
        Assert.Equal(expected.GetYFromX(midpoint), actual.GetYFromX(midpoint));
    }
}
