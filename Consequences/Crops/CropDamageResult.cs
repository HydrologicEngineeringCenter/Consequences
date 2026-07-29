using Consequences.Receptors;

namespace Consequences.Crops;

/// <summary>
/// Result of computing agricultural damage for a single crop location.
/// ["Crop", "x", "y", "Damage Outcome", "Damage", "Duration", "Arrival Time"]
/// </summary>
public struct CropDamageResult : IConsequenceResult
{
    /// <summary>Name of the crop (e.g., "Corn", "Soybeans")</summary>
    public string Crop { get; init; }

    /// <summary>X coordinate of the crop location</summary>
    public float X { get; init; }

    /// <summary>Y coordinate of the crop location</summary>
    public float Y { get; init; }

    /// <summary>The determined damage case/outcome for this event</summary>
    public CropDamageCase DamageCase { get; init; }

    /// <summary>Estimated economic damage in dollars</summary>
    public double Damage { get; init; }

    /// <summary>Duration of the hazard event in decimal hours</summary>
    public double DurationHours { get; init; }

    /// <summary>Date and time the hazard event arrived</summary>
    public DateTime ArrivalTime { get; init; }

    public CropDamageResult(
        string crop,
        float x,
        float y,
        CropDamageCase damageCase,
        double damage,
        double durationHours,
        DateTime arrivalTime)
    {
        Crop = crop;
        X = x;
        Y = y;
        DamageCase = damageCase;
        Damage = damage;
        DurationHours = durationHours;
        ArrivalTime = arrivalTime;
    }

    /// <summary>
    /// Returns a tabular string representation matching the Go output format.
    /// Useful for debugging and CSV export.
    /// </summary>
    public override string ToString()
    {
        return $"{Crop}, {X}, {Y}, {DamageCase}, {Damage}, {DurationHours}, {ArrivalTime:yyyy-MM-dd HH:mm:ss}";
    }
}