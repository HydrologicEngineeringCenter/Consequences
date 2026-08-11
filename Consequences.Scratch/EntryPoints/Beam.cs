using Consequences.Scratch.Beam;

namespace Consequences.Scratch.EntryPoints;

public static class Beam
{
    public static void EntryPoint()
    {
        // Why INsiStructureMapper<out TReceptor> can't have TryMap(..., out TReceptor).
        //CovarianceExperiments.RunAll();

        // Whether `required` does anything on a struct property, and if it should stay.
        //RequiredOnStructsExperiments.RunAll();

        // Delegating overloads, the `Async` suffix, and eager vs deferred throws.
        AsyncDelegationExperiments.RunAll();
    }
}
