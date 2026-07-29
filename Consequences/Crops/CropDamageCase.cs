using System;

namespace Consequences.Crops;

/// <summary>
/// Possible outcomes when evaluating a crop schedule against a hazard event.
/// </summary>
[Flags]
public enum CropDamageCase : byte
{
    Unassigned = 0x00,
    Impacted = 0x01,
    NotImpactedDuringSeason = 0x02,
    PlantingDelayed = 0x04,
    NotPlanted = 0x08,
    SubstituteCrop = 0x10,
}