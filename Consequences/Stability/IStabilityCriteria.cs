using Consequences.Hazards;

namespace Consequences.Stability;

public interface IStabilityCriteria
{
    bool Collapsed(IHazard hazard);
}
