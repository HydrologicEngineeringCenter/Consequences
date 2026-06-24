namespace Consequences.Receptors;

public readonly record struct DamageResult(
    double Structure,
    double Content,
    double Other,
    double Vehicle)
{
    public double Total => Structure + Content + Other + Vehicle;
}
