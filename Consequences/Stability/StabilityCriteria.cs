using Consequences.Hazards;

namespace Consequences.Stability;

public class StabilityCriteria
{
    private readonly Func<double, double, bool> _predicate;

    public StabilityCriteria(Func<double, double, bool> predicate)
    {
        _predicate = predicate;
    }

    public bool Evaluate(IDepthVelocityHazard hazard) => _predicate(hazard.Depth, hazard.Velocity);

    public bool Evaluate(double depth, double velocity) => _predicate(depth, velocity);

    public static StabilityCriteria DepthVelocityProduct(double threshold) =>
        new((d, v) => d * v < threshold);
}
