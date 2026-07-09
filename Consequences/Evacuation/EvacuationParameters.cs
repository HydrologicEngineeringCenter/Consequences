using Consequences.Stability;
using Numerics.Data;

namespace Consequences.Evacuation;

public class EvacuationParameters
{
    //public bool CollectivelyWarned { get; init; }
    //public bool CollectivelyMobilize { get; init; }
    //public byte VehicleOccupancyRate { get; init; } = 1;
    public float FractionInVehicles { get; init; } = 0.1f;
    public float FractionInCars { get; init; } = 0.5f;

    //public bool SimulateTraffic { get; init; }
    public float FractionTrafficReroute { get; init; } = 0.8f;

    public required StabilityThreshold LowClearanceStability { get; init; }
    public required StabilityThreshold HighClearanceStability { get; init; }
    public required StabilityThreshold PedestrianStability { get; init; }

    public required OrderedPairedData LowClearanceRoadEntryCdf { get; init; }
    public required OrderedPairedData HighClearanceRoadEntryCdf { get; init; }
}
