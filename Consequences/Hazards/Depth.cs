namespace Consequences.Hazards;

public struct DepthHazard : IDepthHazard
{
    public double Depth { get; set; }

    public DepthHazard(double depth)
    {
        Depth = depth;
    }
}
