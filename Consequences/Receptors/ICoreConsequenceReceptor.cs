using Consequences.Hazards;

namespace Consequences.Receptors;

public interface ICoreConsequenceReceptor<TResult> where TResult : struct
{
    // Alternative 2: struct return per call.
    TResult ComputeComponents<THazard>(THazard hazard) where THazard : IDepthVelocityHazard;
    TResult ComputeComponents(double depth);
    TResult ComputeComponents(double depth, double velocity);

    // Alternative 4: caller-allocated result filled via out.
    void Compute<THazard>(THazard hazard, out TResult result) where THazard : IDepthVelocityHazard;
    void Compute(double depth, out TResult result);
    void Compute(double depth, double velocity, out TResult result);

    // Alternative 5: total only, no components surfaced.
    double Compute<THazard>(THazard hazard) where THazard : IDepthVelocityHazard;
    double Compute(double depth);
    double Compute(double depth, double velocity);
}
