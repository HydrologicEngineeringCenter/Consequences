using Consequences.Hazards;
using System.Collections.Generic;
using System.Linq;

namespace Consequences.Crops;

/// <summary>
/// Manages duration-to-damage curves by month.

/// </summary>
public struct DamageFunction
{
    /// <summary>
    /// Maps hazard duration (hours) to an array of 12 monthly damage percentages.
    /// Keys represent duration thresholds; values are [Jan..Dec] damage %.
    /// </summary>
    public Dictionary<double, double[]> DurationDamageCurves { get; init; }

    /// <summary>
    /// Computes damage percentage by linearly interpolating between duration buckets
    /// for the month of the hazard arrival.
    /// </summary>
    public double ComputeDamagePercent(ArrivalDurationHazard h)
    {
        double previousKey = 0.0;
        double[] previousValue = new double[12];
        bool firstIteration = true;
        int hazardMonthIndex = h.MonthIndex;

        var sortedKeys = DurationDamageCurves.Keys.OrderBy(k => k).ToList();

        foreach (var k in sortedKeys)
        {
            var v = DurationDamageCurves[k];
            if (k > h.DurationHours)
            {
                if (firstIteration)
                {
                    // Below the first bucket: interpolate down to zero
                    double factor = h.DurationHours / k;
                    return v[hazardMonthIndex] * factor;
                }

                // Between two buckets: linear interpolation
                double factor = (k - h.DurationHours) / (k - previousKey);
                return previousValue[hazardMonthIndex] + factor * (v[hazardMonthIndex] - previousValue[hazardMonthIndex]);
            }

            previousKey = k;
            previousValue = v;
            firstIteration = false;
        }

        // Exceeds all buckets: return the highest known damage percentage
        return previousValue[hazardMonthIndex];
    }
}