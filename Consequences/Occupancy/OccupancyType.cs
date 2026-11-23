using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Numerics.Data;

namespace Consequences.Occupancy;

public class OccupancyType
{
    public required string Name { get; init; }

    public required OrderedPairedData StructureDamageFunction { get; init; }
    public required OrderedPairedData ContentDamageFunction { get; init; }

    public float FoundationHeightOffset { get; init; }

    public float StructureValuePercentageOfTheMean { get; init; } = 1.0f;
    public float ContentValuePercentageOfTheMean { get; init; } = 1.0f;

    public bool CollectivelyWarned { get; init; } 
    public bool CollectivelyMobilize {get; init;}

    public byte VehicleOccupancyRate { get; init; } = 2;

    public float FromFloorDepthThreshold { get; init; }
    public float FromCeilingDepthThreshold { get; init; }

    public XElement SaveToXElement()
    {
        return new XElement("OccupancyType",
            new XAttribute("Name", Name),
            new XAttribute("FoundationHeightOffset", FoundationHeightOffset),
            new XAttribute("StructureValuePercentageOfTheMean", StructureValuePercentageOfTheMean),
            new XAttribute("ContentValuePercentageOfTheMean", ContentValuePercentageOfTheMean),
            new XAttribute("CollectivelyWarned", CollectivelyWarned),
            new XAttribute("CollectivelyMobilize", CollectivelyMobilize),
            new XAttribute("VehicleOccupancyRate", VehicleOccupancyRate),
            new XAttribute("FromFloorDepthThreshold", FromFloorDepthThreshold),
            new XAttribute("FromCeilingDepthThreshold", FromCeilingDepthThreshold),
            new XElement("StructureDamageFunction", StructureDamageFunction.SaveToXElement()),
            new XElement("ContentDamageFunction", ContentDamageFunction.SaveToXElement()));
    }

    public static OccupancyType FromXElement(XElement element)
    {
        return new OccupancyType
        {
            Name = RequiredAttribute(element, "Name").Value,
            StructureDamageFunction = ReadDamageFunction(RequiredElement(element, "StructureDamageFunction")),
            ContentDamageFunction = ReadDamageFunction(RequiredElement(element, "ContentDamageFunction")),
            FoundationHeightOffset = (float?)element.Attribute("FoundationHeightOffset") ?? 0f,
            StructureValuePercentageOfTheMean = (float?)element.Attribute("StructureValuePercentageOfTheMean") ?? 1f,
            ContentValuePercentageOfTheMean = (float?)element.Attribute("ContentValuePercentageOfTheMean") ?? 1f,
            CollectivelyWarned = (bool?)element.Attribute("CollectivelyWarned") ?? false,
            CollectivelyMobilize = (bool?)element.Attribute("CollectivelyMobilize") ?? false,
            VehicleOccupancyRate = checked((byte)((int?)element.Attribute("VehicleOccupancyRate") ?? 2)),
            FromFloorDepthThreshold = (float?)element.Attribute("FromFloorDepthThreshold") ?? 0f,
            FromCeilingDepthThreshold = (float?)element.Attribute("FromCeilingDepthThreshold") ?? 0f
        };
    }

    private static OrderedPairedData ReadDamageFunction(XElement element)
    {
        XElement curve = RequiredElement(element, "OrderedPairedData");

        // Check required values before Numerics can substitute defaults.
        bool.Parse(RequiredAttribute(curve, "StrictX").Value);
        bool.Parse(RequiredAttribute(curve, "StrictY").Value);
        Enum.Parse<SortOrder>(RequiredAttribute(curve, "OrderX").Value);
        Enum.Parse<SortOrder>(RequiredAttribute(curve, "OrderY").Value);

        foreach (XElement point in RequiredElement(curve, "Ordinates").Elements("Ordinate"))
        {
            double.Parse(RequiredAttribute(point, "X").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
            double.Parse(RequiredAttribute(point, "Y").Value, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        return new OrderedPairedData(curve);
    }

    private static XAttribute RequiredAttribute(XElement element, string name)
    {
        return element.Attribute(name)
            ?? throw new XmlException($"Required attribute '{name}' is missing from '{element.Name}'.");
    }

    private static XElement RequiredElement(XElement element, string name)
    {
        return element.Element(name)
            ?? throw new XmlException($"Required element '{name}' is missing from '{element.Name}'.");
    }

}
