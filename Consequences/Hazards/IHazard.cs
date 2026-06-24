namespace Consequences.Hazards;

public interface IHazard
{
    double Depth { get; }
    double Velocity { get; }
    double Duration { get; }
}
