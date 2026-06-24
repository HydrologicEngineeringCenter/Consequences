namespace Consequences.Hazards;

public struct DepthVelocity : IDepthVelocityHazard
{
    public double Depth { get; set; }
    public double Velocity { get; set; }
    public DepthVelocity(double depth, double velocity)
    {
        Depth = depth;
        Velocity = velocity;
    }
}
