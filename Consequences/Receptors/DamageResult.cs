namespace Consequences.Receptors;

public readonly record struct DamageResult(
    float Structure,
    float Content) : IConsequenceResult
{
    public const int ComponentCount = 2;

    public float Total => Structure + Content;
}
