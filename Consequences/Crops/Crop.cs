using Consequences.Hazards;

namespace Consequences.Crops;

/// <summary>
/// Agricultural consequence receptor implementing the generic computation pattern.
/// </summary>
public struct Crop : IConsequenceReceptor<ArrivalDurationHazard, CropDamageResult>
{
    public byte Id { get; init; }
    public required string Name { get; init; }
    public string SubstituteName { get; init; }
    public Crop? SubstituteCrop { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public double TotalMarketValue { get; init; }
    public ProductionFunction ProductionFunction { get; init; }
    public DamageFunction DamageFunction { get; init; }
    public CropSchedule Schedule { get; init; }

    public CropDamageResult Compute(ArrivalDurationHazard hazard)
    {
        var outcome = Schedule.ComputeCropDamageCase(hazard);
        double damage = 0.0;

        switch (outcome)
        {
            case CropDamageCase.Impacted:
                damage = ComputeImpactedCase(hazard);
                break;
            case CropDamageCase.PlantingDelayed:
                damage = ComputeDelayedCase(hazard);
                break;
            case CropDamageCase.NotPlanted:
                (damage, outcome) = ComputeNotPlantedCase(hazard);
                break;
            case CropDamageCase.SubstituteCrop:
                damage = ComputeSubstituteCase(hazard);
                break;
            case CropDamageCase.Unassigned:
            case CropDamageCase.NotImpactedDuringSeason:
            default:
                damage = 0.0;
                break;
        }

        return new CropDamageResult(
            Name, X, Y, outcome, damage, hazard.DurationHours, hazard.ArrivalTime);
    }

    private double ComputeImpactedCase(ArrivalDurationHazard h)
    {
        double dmgFactor = DamageFunction.ComputeDamagePercent(h) / 100.0;
        double exposedProductionValue = ProductionFunction.GetExposedValue(h);
        double totalProductionCost = ProductionFunction.ProductionCostLessHarvest;
        double percentProductionValue = totalProductionCost != 0 
            ? exposedProductionValue / totalProductionCost 
            : 0.0;
        double totalMarketValueLessHarvestCost = TotalMarketValue - ProductionFunction.HarvestCost;
        
        return dmgFactor * percentProductionValue * totalMarketValueLessHarvestCost;
    }

    private double ComputeDelayedCase(ArrivalDurationHazard h)
    {
        double plantingWindow = (Schedule.LastPlantingDate - Schedule.StartPlantingDate).TotalDays;
        if (plantingWindow <= 0) return 0.0;

        DateTime actualPlant = h.ArrivalTime.AddHours(h.DurationHours);
        double daysLate = (actualPlant - new DateTime(actualPlant.Year, 1, 1)).TotalDays;
        double factor = (daysLate / plantingWindow) * (ProductionFunction.LossFromLatePlanting / 100.0);
        
        return TotalMarketValue * factor;
    }

    private (double Damage, CropDamageCase Outcome) ComputeNotPlantedCase(ArrivalDurationHazard h)
    {
        if (string.IsNullOrEmpty(SubstituteName))
        {
            // Loss is only fixed costs accumulated over the year
            return (ProductionFunction.CumulativeMonthlyFixedCostsOnly[11], CropDamageCase.NotPlanted);
        }
        
        return (ComputeSubstituteCase(h), CropDamageCase.SubstituteCrop);
    }

    private double ComputeSubstituteCase(ArrivalDurationHazard h)
    {
        if (SubstituteCrop == null) return 0.0;
        
        double originalValue = TotalMarketValue - ProductionFunction.HarvestCost;
        var sc = SubstituteCrop.Value;
        double substituteValue = sc.TotalMarketValue - sc.ProductionFunction.HarvestCost;
        
        return substituteValue <= originalValue ? originalValue - substituteValue : 0.0;
    }
}