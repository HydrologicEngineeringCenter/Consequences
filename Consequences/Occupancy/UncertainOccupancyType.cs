namespace Consequences.Occupancy;

public class UncertainOccupancyType
{
    public string? Name { get; init; }

    public object? UncertainStructureDamageFunction { get; init; }
    public object? UncertainContentDamageFunction { get; init; }
    public object? UncertainOtherDamageFunction { get; init; }
    public object? UncertainVehicleDamageFunction { get; init; }

    public object? FoundationHeightUncertainty { get; init; }
    public object? StructureValueUncertainty { get; init; }
    public object? ContentValueUncertainty { get; init; }
    public object? OtherValueUncertainty { get; init; }
    public object? VehicleValueUncertainty { get; init; }

    public object? EvacuatingGroupSize { get; init; }
    public object? FractionPopulationToRoofOrAttic { get; init; }
    public object? PopulationReceivesWarningAtSameTime { get; init; }
    public object? PopulationMobilizesAtSameTime { get; init; }
    public object? SubmergenceCriteria { get; init; }
}
