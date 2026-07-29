namespace Consequences.Hazards;

/// <summary>
/// A hazard characterized by an arrival time and duration.
/// Primary receptor: crops (agricultural damage assessment).
///
/// Crop damage depends on when a flood arrives relative to the planting
/// season and how long it persists. 
/// </summary>
public struct ArrivalDurationHazard : IArrivalDurationHazard
{
    /// <summary>
    /// The date and time when the hazard event begins.
    /// </summary>
    public DateTime ArrivalTime { get; init; }

    /// <summary>
    /// The duration of the hazard event in decimal hours.
    /// </summary>
    public double DurationHours { get; init; }

 
    /// <summary>
    /// Creates an ArrivalDurationHazard with depth set to zero.
    /// </summary>
    public ArrivalDurationHazard(DateTime arrivalTime, double durationHours)
        : this(arrivalTime, durationHours, 0f)
    {
    }

    /// <summary>
    /// Returns the day-of-year (1-366) for the arrival time.
    /// Used by crop schedule logic to determine seasonal position.
    /// </summary>
    public int DayOfYear => ArrivalTime.DayOfYear;

    /// <summary>
    /// Returns the duration of the hazard event in decimal days.
    /// </summary>
    public double DurationDays => DurationHours / 24.0;

    /// <summary>
    /// Returns the date and time when the hazard event ends.
    /// </summary>
    public DateTime DepartureTime => ArrivalTime.AddHours(DurationHours);

    /// <summary>
    /// Returns the 0-based month index (0=January, 11=December)
    /// for the arrival time. Used to index into duration-damage curves.
    /// </summary>
    public int MonthIndex => (int)ArrivalTime.Month - 1;

    /// <summary>
    /// Constructs an ArrivalDurationHazard from a HydraulicTimeSeries
    /// by extracting the first non-zero depth time step as arrival
    /// and the total inundated duration as duration.
    /// </summary>
    public static ArrivalDurationHazard FromHydraulicTimeSeries(HydraulicTimeSeries hts)
    {
        // Find first time step where depth > 0
        int arrivalIndex = -1;
        for (int i = 0; i < hts.Depths.Length; i++)
        {
            if (hts.Depths[i] > 0f)
            {
                arrivalIndex = i;
                break;
            }
        }

        if (arrivalIndex < 0)
        {
            // No inundation -- return a zero-duration hazard at time zero
            return new ArrivalDurationHazard(
                DateTime.UtcNow.Date, 0.0, 0f);
        }

        float arrivalMinutes = hts.TimeMinutes[arrivalIndex];
        float departureMinutes = hts.TimeMinutes[^1];
        double durationHours = (departureMinutes - arrivalMinutes) / 60.0;//what about if the water recedes below the cell during the event?

        // ArrivalTime is relative to hydraulic start; callers should
        // add the simulation start date. Here we use the arrival offset.
        DateTime arrivalTime = DateTime.UtcNow.Date.AddMinutes(arrivalMinutes);

        return new ArrivalDurationHazard(arrivalTime, durationHours);
    }

    /// <summary>
    /// Constructs an ArrivalDurationHazard from a HydraulicTimeSeries
    /// with an explicit simulation start date.
    /// </summary>
    /// <param name="hts">The hydraulic time series data.</param>
    /// <param name="simStartDate">The base date for the hydraulic simulation.</param>
    public static ArrivalDurationHazard FromHydraulicTimeSeries(
        HydraulicTimeSeries hts, DateTime simStartDate)
    {
        int arrivalIndex = -1;
        for (int i = 0; i < hts.Depths.Length; i++)
        {
            if (hts.Depths[i] > 0f)
            {
                arrivalIndex = i;
                break;
            }
        }

        if (arrivalIndex < 0)
        {
            return new ArrivalDurationHazard(simStartDate, 0.0, 0f);
        }


        float arrivalMinutes = hts.TimeMinutes[arrivalIndex];
        float departureMinutes = hts.TimeMinutes[^1];
        double durationHours = (departureMinutes - arrivalMinutes) / 60.0;//what about if the water recedes below the cell during the event?

        DateTime arrivalTime = simStartDate.AddMinutes(arrivalMinutes);

        return new ArrivalDurationHazard(arrivalTime, durationHours);
    }

    /// <summary>
    /// Returns true if the hazard event overlaps the given date range
    /// [startDate, endDate).
    /// </summary>
    public bool Overlaps(DateTime startDate, DateTime endDate)
    {
        return ArrivalTime < endDate && DepartureTime > startDate;
    }

    /// <summary>
    /// Returns a string representation of the hazard for debugging.
    /// </summary>
    public override string ToString()
    {
        return $"Arrival: {ArrivalTime:yyyy-MM-dd HH:mm}, " +
               $"Duration: {DurationHours:F2}h ({DurationDays:F2}d), ";
    }
}