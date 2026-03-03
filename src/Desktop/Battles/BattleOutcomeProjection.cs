namespace GalacticTrader.Desktop.Battles;

public sealed class BattleOutcomeProjection
{
    public int ReputationDelta { get; init; }
    public decimal EnvironmentalModifier { get; init; }
    public decimal ProtectionModifier { get; init; }
    public decimal EconomicImpactProjection { get; init; }
    public required string DamageReport { get; init; }
}
