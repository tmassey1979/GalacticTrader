namespace GalacticTrader.Desktop.Modules;

public sealed class AnalyticsSnapshot
{
    public decimal RevenueVolume { get; init; }
    public decimal RevenuePerHour { get; init; }
    public int TradeCount { get; init; }
    public decimal AverageTradeSize { get; init; }
    public int CombatCount { get; init; }
    public int AverageCombatDurationSeconds { get; init; }
    public decimal InsurancePayoutTotal { get; init; }
    public decimal RiskAdjustedReturn { get; init; }
    public decimal BattleToProfitRatio { get; init; }
    public decimal RoiPerShip { get; init; }
    public decimal MarketSharePercent { get; init; }
    public decimal SystemInfluencePercent { get; init; }
}
