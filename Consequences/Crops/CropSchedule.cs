using Consequences.Hazards;

namespace Consequences.Crops;

/// <summary>
/// Stores planting season boundaries and time to maturity.
/// Evaluates hazard arrival/duration against the crop calendar to produce a damage case.
/// </summary>
public struct CropSchedule
{
    public DateTime StartPlantingDate { get; init; }
    public DateTime LastPlantingDate { get; init; }
    public int DaysToMaturity { get; init; }

    /// <summary>
    /// Evaluates a hazard against the crop calendar.
    /// </summary>
    public CropDamageCase ComputeCropDamageCase(ArrivalDurationHazard h)
    {
        int hazardStartDoy = h.DayOfYear;
        int hazardDurationDays = (int)h.DurationDays;
        int startDoy = StartPlantingDate.DayOfYear;
        int lastDoy = LastPlantingDate.DayOfYear;

        if (hazardStartDoy <= startDoy)
        {
            // Hazard arrives at or before planting window
            if (hazardStartDoy + hazardDurationDays < startDoy)
            {
                // Hazard ends before planting begins. Check for winter crops that harvest early next year.
                if (startDoy + DaysToMaturity > 365)
                {
                    int harvestDoY = startDoy + DaysToMaturity - 365;
                    if (harvestDoY > hazardStartDoy)
                        return CropDamageCase.Impacted;
                    return CropDamageCase.NotImpactedDuringSeason;
                }
                return CropDamageCase.NotImpactedDuringSeason;
            }

            // Hazard overlaps planting window
            if (hazardStartDoy + hazardDurationDays < lastDoy)
                return CropDamageCase.PlantingDelayed;
            
            return CropDamageCase.NotPlanted;
        }

        // Hazard arrives after planting window starts
        if (startDoy + DaysToMaturity < hazardStartDoy)
            return CropDamageCase.NotImpactedDuringSeason; // After harvest

        return CropDamageCase.Impacted;
    }
}