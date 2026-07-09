using Consequences.Buildings;
using Consequences.Hazards;
using Numerics.Data;

namespace Consequences.Stability;

public class StabilityThreshold
{
    private string _name = string.Empty;
    private readonly OrderedPairedData _threshold;
    public StabilityThreshold(OrderedPairedData threshold)
    {
        _threshold = threshold;
    }

    public bool Collapsed(DepthVelocity depthVelocity)
    {
        return Collapsed(depthVelocity.Depth, depthVelocity.Velocity, _threshold);
    }
    public bool Collapsed(float depth, float velocity)
    {
        return Collapsed(depth, velocity, _threshold);
    }

    public static bool Collapsed(float depth, float velocity, OrderedPairedData threshold)
    {
        return threshold.GetYFromX(velocity) <= depth;
    }

    public bool Collapsed(HydraulicTimeSeries hydTimeSeries, float foundationHeight)
    {
        return Collapsed(hydTimeSeries.Depths, hydTimeSeries.Velocities, foundationHeight, _threshold);
    }
    public static bool Collapsed(float[] depths, float[] velocities, float foundationHeight, OrderedPairedData threshold)
    {
        for (int i = 0; i < depths.Length; i++)
        {
            if (velocities[i] < threshold[0].X) { continue; } //below lowest velocity & depth
            if (velocities[i] > threshold[threshold.Count - 1].X) { return true; } //above highest velocity COLLAPSED
            if (depths[i] < threshold[threshold.Count - 1].Y) { continue; }// below lowest depth
            if (depths[i] > threshold[0].Y) { return true; }//above highest depth COLLAPSED

            if (threshold.GetYFromX(velocities[i]) <= (depths[i] - foundationHeight)) { return true; }
        }
        return false;
    }
}
