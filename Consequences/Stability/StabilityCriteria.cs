using Consequences.Buildings;
using Consequences.Hazards;
using Numerics.Data;

namespace Consequences.Stability;

public class StabilityCriteria
{
    private string _name = string.Empty;
    private readonly OrderedPairedData _threshold;

    public StabilityCriteria(OrderedPairedData threshold)
    {
        _threshold = threshold;
    }

    public bool Collapsed(DepthVelocity depthVelocity, Building building)
    {
        return Collapsed(depthVelocity.Depth, depthVelocity.Velocity, _threshold, building);
    }
    public static bool Collapsed(double depth, double velocity, OrderedPairedData threshold, Building building)
    {
        return threshold.GetYFromX(velocity) <= depth - building.FoundationHeight;
    }

    
    public bool Collapsed(HydraulicTimeSeries hydTimeSeries, Building building)
    {
        return Collapsed(hydTimeSeries.Depths, hydTimeSeries.Velocities, _threshold, building);
    }
    public static bool Collapsed(float[] depths, float[] velocities, OrderedPairedData threshold, Building building)
    {
        for (int i = 0; i < depths.Length; i++)
        {
            if (velocities[i] < threshold[0].X) { continue; } //below lowest velocity & depth
            if (velocities[i] > threshold[threshold.Count - 1].X) { return true; } //above highest velocity COLLAPSED
            if (depths[i] < threshold[threshold.Count - 1].Y) { continue; }// below lowest depth
            if (depths[i] > threshold[0].Y) { return true; }//above highest depth COLLAPSED

            if (threshold.GetYFromX(velocities[i]) <= (depths[i] - building.FoundationHeight)) { return true; }
        }
        return false;
    }
}
