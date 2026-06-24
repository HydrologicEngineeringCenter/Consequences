using Consequences.Hazards;
using Consequences.Receptors;

namespace Consequences.Buildings;

public static class BuildingBatch
{
    // Alternative 6: parallel-arrays batched compute. Each building has its own
    // hazard at the matching index. The library allocates the result array and
    // returns it as a Span.
    public static Span<DamageResult> ComputeBatch(
        ReadOnlySpan<Building> buildings,
        ReadOnlySpan<Hazard> hazards)
    {
        if (hazards.Length != buildings.Length)
            throw new ArgumentException(
                "hazards span must be the same length as buildings span",
                nameof(hazards));

        int n = buildings.Length;
        // Skip zero-init: every slot is written before the array escapes.
        var results = GC.AllocateUninitializedArray<DamageResult>(n);
        for (int i = 0; i < n; i++)
        {
            var h = hazards[i];
            // Compute writes straight into the array slot — no intermediate temp.
            buildings[i].Compute(h.Depth, h.Velocity, out results[i]);
        }
        return results;
    }

    // Alternative 7: batched total only — no per-building materialization,
    // folds totals as it walks the parallel arrays.
    public static double ComputeBatchTotal(
        ReadOnlySpan<Building> buildings,
        ReadOnlySpan<Hazard> hazards)
    {
        if (hazards.Length != buildings.Length)
            throw new ArgumentException(
                "hazards span must be the same length as buildings span",
                nameof(hazards));

        int n = buildings.Length;
        double total = 0;
        for (int i = 0; i < n; i++)
        {
            var h = hazards[i];
            total += buildings[i].Compute(h.Depth, h.Velocity);
        }
        return total;
    }
}
