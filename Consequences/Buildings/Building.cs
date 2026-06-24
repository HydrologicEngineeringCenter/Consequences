using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Receptors;
using Consequences.Stability;

namespace Consequences.Buildings;

public struct Building : ICoreConsequenceReceptor
{
    public required OccupancyType OccupancyType { get; init; }

    public double StructureValue { get; init; }
    public double ContentValue { get; init; }
    //public double OtherValue { get; init; }
    //public double VehicleValue { get; init; }

    public double FoundationHeight { get; init; }
    public int NumStories { get; init; }
    public double FloorHeight { get; init; } 

    public int AbleBodiedPeople { get; init; }
    public int LimitedMobilityPeople { get; init; }

    public StabilityCriteria? SampledStabilityCriteria { get; init; }

    public DamageResult ComputeComponents(double depth)
    {
        var occ = OccupancyType;
        double effectiveDepth = depth - FoundationHeight - occ.FoundationHeightOffset;

        double structureValue = StructureValue * occ.StructureValuePercentageOfTheMean;
        double contentValue = ContentValue * occ.ContentValuePercentageOfTheMean;
        double otherValue = OtherValue * occ.OtherValuePercentageOfTheMean;
        double vehicleValue = VehicleValue * occ.VehicleValuePercentageOfTheMean;

        return new DamageResult(
            structureValue * occ.StructureDamageFunction(effectiveDepth),
            contentValue * occ.ContentDamageFunction(effectiveDepth),
            otherValue * occ.OtherDamageFunction(effectiveDepth),
            vehicleValue * occ.VehicleDamageFunction(effectiveDepth));
    }

    public DamageResult ComputeComponents(double depth, double velocity) => ComputeComponents(depth);

    public DamageResult ComputeComponents(IHazard hazard) => ComputeComponents(hazard.Depth, hazard.Velocity);

    public double Compute(double depth) => ComputeComponents(depth).Total;
    public double Compute(double depth, double velocity) => ComputeComponents(depth, velocity).Total;
    public double Compute(IHazard hazard) => ComputeComponents(hazard).Total;
}
