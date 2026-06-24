namespace Consequences.Receptors;

public interface IUncertainConsequenceReceptor<TResult> where TResult : struct
{
    ICoreConsequenceReceptor<TResult> Sample();
}
