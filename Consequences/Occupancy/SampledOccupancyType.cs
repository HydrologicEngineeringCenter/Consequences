namespace Consequences.Occupancy;

public class OccupancyType
{
    public required string Name { get; init; }

    public required Func<double, double> StructureDamageFunction { get; init; }
    public required Func<double, double> ContentDamageFunction { get; init; }
    public required Func<double, double> OtherDamageFunction { get; init; }
    public required Func<double, double> VehicleDamageFunction { get; init; }

    public double FoundationHeightOffset { get; init; }

    public double StructureValuePercentageOfTheMean { get; init; } = 1.0;
    public double ContentValuePercentageOfTheMean { get; init; } = 1.0;
    public double OtherValuePercentageOfTheMean { get; init; } = 1.0;
    public double VehicleValuePercentageOfTheMean { get; init; } = 1.0;
}
