namespace Consequences.Hazards;

public interface IDepthVelocityHazard : IDepthHazard
{
    double Velocity { get; }
}
