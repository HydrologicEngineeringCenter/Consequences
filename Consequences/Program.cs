using Consequences.Hazards;
using Consequences.Occupancy;
using Consequences.Stability;
using Consequences.Structures;

static double Linear(double depth) => Math.Clamp(depth / 10.0, 0.0, 1.0);

var residential = new OccupancyType
{
    Name = "RES1-1S-NB",
    StructureDamageFunction = Linear,
    ContentDamageFunction = d => Math.Clamp(d / 8.0, 0.0, 1.0),
    OtherDamageFunction = d => Math.Clamp(d / 12.0, 0.0, 1.0),
    VehicleDamageFunction = d => d > 1.0 ? 1.0 : 0.0,
    FoundationHeightOffset = 0.0,
};

var structure = new Building
{
    OccupancyType = residential,
    StructureValue = 200_000,
    ContentValue = 100_000,
    OtherValue = 25_000,
    VehicleValue = 30_000,
    FoundationHeight = 1.5,
    NumStories = 1,
    FloorHeight = new[] { 9.0 },
    AbleBodiedPeople = 3,
    LimitedMobilityPeople = 1,
    SampledStabilityCriteria = StabilityCriteria.DepthVelocityProduct(threshold: 6.0),
};

var hazards = new Hazard[]
{
    new(depth: 2.0, velocity: 1.0, duration: 1.0),
    new(depth: 6.0, velocity: 2.0, duration: 2.0),
    new(depth: 12.0, velocity: 4.0, duration: 3.0),
};

Console.WriteLine($"Structure: {structure.OccupancyType.Name}");
Console.WriteLine($"  Values: S=${structure.StructureValue:N0} C=${structure.ContentValue:N0} O=${structure.OtherValue:N0} V=${structure.VehicleValue:N0}");
Console.WriteLine($"  FoundationHeight={structure.FoundationHeight} ft");
Console.WriteLine();

foreach (var hazard in hazards)
{
    var total = structure.Compute(hazard);
    var components = structure.ComputeComponents(hazard);
    bool stable = structure.SampledStabilityCriteria!.Evaluate(hazard);

    Console.WriteLine($"Hazard depth={hazard.Depth} ft, vel={hazard.Velocity} ft/s, dur={hazard.Duration} hr");
    Console.WriteLine($"  Total damage          : ${total:N2}");
    Console.WriteLine($"  Components            : S=${components.Structure:N2} C=${components.Content:N2} O=${components.Other:N2} V=${components.Vehicle:N2} (sum=${components.Total:N2})");
    Console.WriteLine($"  Stable (d*v < 6.0)?   : {stable}");
    Console.WriteLine();
}

double depthOnly = structure.Compute(depth: 4.0);
double depthAndVel = structure.Compute(depth: 4.0, velocity: 3.0);
bool stableRaw = structure.SampledStabilityCriteria!.Evaluate(depth: 4.0, velocity: 3.0);
Console.WriteLine($"Raw (depth=4): total=${depthOnly:N2}");
Console.WriteLine($"Raw (depth=4, vel=3): total=${depthAndVel:N2}, stable={stableRaw}");
