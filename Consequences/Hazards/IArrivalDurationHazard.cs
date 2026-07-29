namespace Consequences.Hazards;

/// <summary>
/// Marker interface for hazards characterized by arrival time and duration.
/// Used by agricultural receptors (crops) to determine seasonal impact.
/// </summary>
public interface IArrivalDurationHazard : IHazard
{
    /// <summary>
    /// The date and time when the hazard event begins.
    /// </summary>
    public DateTime ArrivalTime { get; }

    /// <summary>
    /// The duration of the hazard event in decimal hours.
    /// </summary>
    public double DurationHours { get; }
}