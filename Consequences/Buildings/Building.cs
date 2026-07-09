using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;
using Consequences.Stability;

namespace Consequences.Buildings;

public struct Building : IConsequenceReceptor<DepthHazard, DamageResult>
{
    public required OccupancyType OccupancyType { get; init; }

    public float Value { get; init; }
    public float ContentValue { get; init; }

    public float FoundationHeight { get; init; }
    
    public StabilityThreshold? StabilityThreshold { get; init; }


    public DamageResult Compute(float depth) => Compute(depth, this);
    public DamageResult Compute(DepthHazard depth) => Compute(depth.Depth, this);
    public static DamageResult Compute(float depth, Building building)
    {
        var occ = building.OccupancyType;
        float effectiveDepth = depth - building.FoundationHeight - building.OccupancyType.FoundationHeightOffset;

        float structureValue = building.Value * occ.StructureValuePercentageOfTheMean;
        float contentValue = building.ContentValue * occ.ContentValuePercentageOfTheMean;

        return new DamageResult(
            structureValue * (float)occ.StructureDamageFunction.GetYFromX(effectiveDepth),
            contentValue * (float)occ.ContentDamageFunction.GetYFromX(effectiveDepth));
    }

    public DamageResult Compute(float depth, float velocity) => Compute(depth, velocity, this);
    public DamageResult Compute(DepthVelocity depthVelocity) => Compute(depthVelocity.Depth, depthVelocity.Velocity, this);
    public DamageResult Compute(HydraulicTimeSeries depthVelocity) => Compute(depthVelocity.MaxDepth, depthVelocity.MaxVelocity, this);

    public static DamageResult Compute(float depth, float velocity, Building building)
    {
        var occ = building.OccupancyType;
        float effectiveDepth = depth - building.FoundationHeight - building.OccupancyType.FoundationHeightOffset;
        if (effectiveDepth <= 0)
            return new(0, 0);
        float structureValue = building.Value * occ.StructureValuePercentageOfTheMean;
        float contentValue = building.ContentValue * occ.ContentValuePercentageOfTheMean;
        if (building.StabilityThreshold != null && building.StabilityThreshold.Collapsed(effectiveDepth, velocity))
        {
            return new DamageResult(structureValue, contentValue);
        }
        return new DamageResult(
            structureValue * (float)occ.StructureDamageFunction.GetYFromX(effectiveDepth),
            contentValue * (float)occ.ContentDamageFunction.GetYFromX(effectiveDepth));
    }

    //This is our bare metal baseline. don't delete
    public static float ComputeMetal(float depth, Building building)
    {
        var occ = building.OccupancyType;
        float effectiveDepth = depth - building.FoundationHeight - occ.FoundationHeightOffset;

        float structureValue = building.Value * occ.StructureValuePercentageOfTheMean;
        float contentValue = building.ContentValue * occ.ContentValuePercentageOfTheMean;

        return (
            structureValue * (float)occ.StructureDamageFunction.GetYFromX(effectiveDepth) +
            contentValue * (float)occ.ContentDamageFunction.GetYFromX(effectiveDepth));
    }
}
