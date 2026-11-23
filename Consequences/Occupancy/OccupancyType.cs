using System.Text.Json.Serialization;
using Consequences.Serialization;
using Numerics.Data;

namespace Consequences.Occupancy;

public class OccupancyType
{
    [JsonPropertyName("Name")]
    public required string Name { get; init; }

    [JsonPropertyName("StructureDamageFunction")]
    [JsonConverter(typeof(OrderedPairedDataJsonConverter))]
    public required OrderedPairedData StructureDamageFunction { get; init; }

    [JsonPropertyName("ContentDamageFunction")]
    [JsonConverter(typeof(OrderedPairedDataJsonConverter))]
    public required OrderedPairedData ContentDamageFunction { get; init; }

    [JsonPropertyName("FoundationHeightOffset")]
    public float FoundationHeightOffset { get; init; }

    [JsonPropertyName("StructureValuePercentageOfTheMean")]
    public float StructureValuePercentageOfTheMean { get; init; } = 1.0f;
    [JsonPropertyName("ContentValuePercentageOfTheMean")]
    public float ContentValuePercentageOfTheMean { get; init; } = 1.0f;

    [JsonPropertyName("CollectivelyWarned")]
    public bool CollectivelyWarned { get; init; } 
    [JsonPropertyName("CollectivelyMobilize")]
    public bool CollectivelyMobilize {get; init;}

    [JsonPropertyName("VehicleOccupancyRate")]
    public byte VehicleOccupancyRate { get; init; } = 2;

    [JsonPropertyName("FromFloorDepthThreshold")]
    public float FromFloorDepthThreshold { get; init; }
    [JsonPropertyName("FromCeilingDepthThreshold")]
    public float FromCeilingDepthThreshold { get; init; }

}
