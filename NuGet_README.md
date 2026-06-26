# USACE.Consequences

A .NET library for computing flood-event consequences — economic damage and life-loss inputs — used in USACE Hydrologic Engineering Center risk and consequences workflows.

The library provides the core domain primitives:

- **Buildings** — `Building`, `LifeLossBuilding` with depth-driven and depth-velocity-driven damage compute paths.
- **Occupancy** — `OccupancyType` and uncertain-parameter variants, with structure/content damage functions.
- **Hazards** — `DepthHazard`, `DepthVelocity`, and `HydraulicTimeSeries` (with point-reduction and time-indexed sampling).
- **Stability** — `StabilityCriteria` for building collapse and population destabilization.
- **Evacuation** — `EmergencyPlanningZone`, `EvacuationParameters`, and `EvacuationGroup` for generating and tracking evacuating populations.

## Install

```sh
dotnet add package USACE.Consequences
```

## Quick example

```csharp
using Consequences.Buildings;
using Consequences.Occupancy;

var building = new Building
{
    OccupancyType = occupancyType,
    Value = 250_000f,
    ContentValue = 125_000f,
    FoundationHeight = 1.5f,
    NumStories = 1,
};

DamageResult damage = building.Compute(depth: 3.2f);
float total = damage.Total;
```

## License

Distributed under the terms of the MIT-style license shipped in this package (`LICENSE.txt`). Copyright US Army Corps of Engineers Hydrologic Engineering Center.

## Source

<https://github.com/HydrologicEngineeringCenter/Consequences>
