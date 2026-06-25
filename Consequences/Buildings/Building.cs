using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;
using Consequences.Stability;

namespace Consequences.Buildings;

public struct Building 
{
    public required OccupancyType OccupancyType { get; init; }

    public double Value { get; init; }
    public double ContentValue { get; init; }

    public double FoundationHeight { get; init; }
    public int NumStories { get; init; }
    public double FloorHeight { get; init; }

    public int AbleBodiedPeople { get; init; }
    public int LimitedMobilityPeople { get; init; }

    public StabilityCriteria? SampledStabilityCriteria { get; init; }

    // Alternative 2: single struct return.
    public static DamageResult Compute(double depth, Building building)
    {
        var occ = building.OccupancyType;
        double effectiveDepth = depth - building.FoundationHeight - building.OccupancyType.FoundationHeightOffset;

        double structureValue = building.Value * occ.StructureValuePercentageOfTheMean;
        double contentValue = building.ContentValue * occ.ContentValuePercentageOfTheMean;

        return new DamageResult(
            structureValue * occ.StructureDamageFunction(effectiveDepth),
            contentValue * occ.ContentDamageFunction(effectiveDepth));
    }

    public static DamageResult Compute(double depth, double velocity, Building building)  {
        var occ = building.OccupancyType;
        double effectiveDepth = depth - building.FoundationHeight - building.OccupancyType.FoundationHeightOffset;

        double structureValue = building.Value * occ.StructureValuePercentageOfTheMean;
        double contentValue = building.ContentValue * occ.ContentValuePercentageOfTheMean;

        return new DamageResult(
            structureValue * occ.StructureDamageFunction(effectiveDepth),
            contentValue * occ.ContentDamageFunction(effectiveDepth));
    }

    public readonly DamageResult Compute<THazard>(THazard hazard) where THazard : IDepthHazard =>
        Compute(hazard.Depth, this);

    // Alternative 5: total only, no components surfaced ****Fastest possible. 
    public readonly double Compute(double depth)
    {
        var occ = OccupancyType;
        double effectiveDepth = depth - FoundationHeight - OccupancyType.FoundationHeightOffset;

        double structureValue = Value * occ.StructureValuePercentageOfTheMean;
        double contentValue = ContentValue * occ.ContentValuePercentageOfTheMean;

        return structureValue * occ.StructureDamageFunction(effectiveDepth)
             + contentValue * occ.ContentDamageFunction(effectiveDepth);
    }
}
