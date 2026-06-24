namespace Consequences.Hazards;

public readonly struct Hazard : IHazard
{
    public Hazard(double depth, double velocity, double duration)
    {
        Depth = depth;
        Velocity = velocity;
        Duration = duration;
    }

    public double Depth { get; }
    public double Velocity { get; }
    public double Duration { get; }
}
