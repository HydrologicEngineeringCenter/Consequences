using Numerics.Data;

namespace Consequences.Occupancy;

public class OccupancyType
{
    public required string Name { get; init; }

    public required Func<float, float> StructureDamageFunction { get; init; }
    public required Func<float, float> ContentDamageFunction { get; init; }

    public float FoundationHeightOffset { get; init; }

    public float StructureValuePercentageOfTheMean { get; init; } = 1.0f;
    public float ContentValuePercentageOfTheMean { get; init; } = 1.0f;




    //ALTERNATIVE

    public required OrderedPairedData StructureDamageFunctionOrdinates { get; init; }
    public required OrderedPairedData ContentDamageFunctionOrdinates { get; init; }
}
