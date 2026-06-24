using Consequences.Hazards;

namespace Consequences.Receptors;

public interface ICoreConsequenceReceptor
{
    double Compute(IHazard hazard);
    double Compute(double depth);
    double Compute(double depth, double velocity);

    DamageResult ComputeComponents(IHazard hazard);
    DamageResult ComputeComponents(double depth);
    DamageResult ComputeComponents(double depth, double velocity);
}
