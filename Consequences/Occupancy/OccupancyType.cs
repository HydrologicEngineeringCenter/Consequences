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

}
