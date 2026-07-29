using Consequences.Hazards;

namespace Consequences.Crops;

/// <summary>
/// Represents the economic costs of producing a crop.
/// </summary>
public struct ProductionFunction
{
    public double HarvestCost { get; init; }
    public double[] CumulativeMonthlyProductionCostsEarly { get; init; }
    public double[] CumulativeMonthlyProductionCostsLate { get; init; }
    public double[] CumulativeMonthlyFixedCostsOnly { get; init; }
    public double ProductionCostLessHarvest { get; init; }
    public double LossFromLatePlanting { get; init; }

    /// <summary>
    /// Returns the cumulative production cost exposed at the month of hazard arrival.
    /// Assumes on-time planting.
    /// </summary>
    public double GetExposedValue(ArrivalDurationHazard h) =>
        CumulativeMonthlyProductionCostsEarly[h.MonthIndex];

    /// <summary>
    /// Factory method mirroring go-consequences/crops/productionfunctions.go:NewProductionFunction
    /// </summary>
    public static ProductionFunction Create(
        double[] monthlyVariableCostsEarly,
        double[] monthlyVariableCostsLate,
        double[] monthlyFixedCosts,
        CropSchedule schedule,
        double harvestCost,
        double lossFromLatePlanting)
    {
        var pf = new ProductionFunction
        {
            HarvestCost = harvestCost,
            LossFromLatePlanting = lossFromLatePlanting
        };

        var earlyResult = CumulateMonthlyCosts(monthlyVariableCostsEarly, monthlyFixedCosts, schedule.StartPlantingDate, schedule.DaysToMaturity);
        pf.CumulativeMonthlyProductionCostsEarly = earlyResult.CumulativeCosts;
        pf.ProductionCostLessHarvest = earlyResult.TotalCosts;

        var lateResult = CumulateMonthlyCosts(monthlyVariableCostsLate, monthlyFixedCosts, schedule.LastPlantingDate, schedule.DaysToMaturity);
        pf.CumulativeMonthlyProductionCostsLate = lateResult.CumulativeCosts;

        var fixedResult = CumulateMonthlyCosts(new double[12], monthlyFixedCosts, schedule.StartPlantingDate, schedule.DaysToMaturity);
        pf.CumulativeMonthlyFixedCostsOnly = fixedResult.CumulativeCosts;

        return pf;
    }

    private static (double TotalCosts, double[] CumulativeCosts) CumulateMonthlyCosts(
        double[] variableCosts, double[] fixedCosts, DateTime startDate, int daysToMaturity)
    {
        double totalCosts = 0.0;
        double[] cumulativeCosts = new double[12];
        int remainingDays = daysToMaturity;
        int currentMonth = startDate.Month - 1;
        int currentYear = startDate.Year;
        bool firstMonth = true;

        while (remainingDays > 0)
        {
            int daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth + 1);
            if (firstMonth)
            {
                daysInMonth -= startDate.Day;
                firstMonth = false;
            }

            remainingDays -= daysInMonth;
            totalCosts += variableCosts[currentMonth] + fixedCosts[currentMonth];
            cumulativeCosts[currentMonth] = totalCosts;

            currentMonth++;
            if (currentMonth > 11)
            {
                currentMonth = 0;
                currentYear++;
            }
        }

        return (totalCosts, cumulativeCosts);
    }
}