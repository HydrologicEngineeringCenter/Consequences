using Consequences.Hazards;
using Consequences.Receptors;

namespace Consequences;

public interface IConsequenceReceptor<THazard, TResult>
    where THazard : struct, IHazard
    where TResult : struct, IConsequenceResult
{
    TResult Compute(THazard hazard);
}
