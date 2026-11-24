using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Numerics.Data;

namespace Consequences.Occupancy;

[XmlRoot("OccupancyType")]
public class OccupancyType
{
    private static readonly XmlSerializer Serializer = new(typeof(OccupancyType));

    [XmlAttribute("Name")]
    public required string Name { get; init; }

    [XmlElement("StructureDamageFunction")]
    public required OrderedPairedData StructureDamageFunction { get; init; }
    [XmlElement("ContentDamageFunction")]
    public required OrderedPairedData ContentDamageFunction { get; init; }

    [XmlAttribute("FoundationHeightOffset")]
    public float FoundationHeightOffset { get; init; }

    [XmlAttribute("StructureValuePercentageOfTheMean")]
    public float StructureValuePercentageOfTheMean { get; init; } = 1.0f;
    [XmlAttribute("ContentValuePercentageOfTheMean")]
    public float ContentValuePercentageOfTheMean { get; init; } = 1.0f;

    [XmlAttribute("CollectivelyWarned")]
    public bool CollectivelyWarned { get; init; } 
    [XmlAttribute("CollectivelyMobilize")]
    public bool CollectivelyMobilize {get; init;}

    [XmlAttribute("VehicleOccupancyRate")]
    public byte VehicleOccupancyRate { get; init; } = 2;

    [XmlAttribute("FromFloorDepthThreshold")]
    public float FromFloorDepthThreshold { get; init; }
    [XmlAttribute("FromCeilingDepthThreshold")]
    public float FromCeilingDepthThreshold { get; init; }

    public XElement SaveToXElement()
    {
        var document = new XDocument();
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", "");
        using (XmlWriter writer = document.CreateWriter())
            Serializer.Serialize(writer, this, namespaces);
        return document.Root!;
    }

    // XmlSerializer does not enforce required members.
    public static OccupancyType FromXElement(XElement element)
    {
        RequireAttribute(element, "Name");
        RequireCurve(RequireElement(element, "StructureDamageFunction"));
        RequireCurve(RequireElement(element, "ContentDamageFunction"));

        using XmlReader reader = element.CreateReader();
        var occupancy = (OccupancyType?)Serializer.Deserialize(reader);
        if (occupancy is null || occupancy.StructureDamageFunction is null || occupancy.ContentDamageFunction is null)
            throw new XmlException("Occupancy type and damage functions must not be null.");
        return occupancy;
    }

    private static void RequireCurve(XElement curve)
    {
        RequireAttribute(curve, "StrictX");
        RequireAttribute(curve, "StrictY");
        RequireAttribute(curve, "OrderX");
        RequireAttribute(curve, "OrderY");
        foreach (XElement point in RequireElement(curve, "Ordinates").Elements("Ordinate"))
        {
            RequireAttribute(point, "X");
            RequireAttribute(point, "Y");
        }
    }

    private static void RequireAttribute(XElement element, string name)
    {
        if (element.Attribute(name) is null)
            throw new XmlException($"Required attribute '{name}' is missing from '{element.Name}'.");
    }

    private static XElement RequireElement(XElement element, string name)
    {
        return element.Element(name)
            ?? throw new XmlException($"Required element '{name}' is missing from '{element.Name}'.");
    }
}
