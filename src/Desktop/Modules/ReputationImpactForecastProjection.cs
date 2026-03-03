namespace GalacticTrader.Desktop.Modules;

public sealed class ReputationImpactForecastProjection
{
    public decimal TradeMarginModifier { get; init; }
    public decimal ProtectionCostModifier { get; init; }
    public decimal SmugglingSuccessChance { get; init; }
    public required string AllianceAccessSummary { get; init; }
}
