namespace GalacticTrader.Desktop.Modules;

public sealed class AnalyticsSnapshot
{
    public decimal RevenueVolume { get; init; }
    public int TradeCount { get; init; }
    public decimal AverageTradeSize { get; init; }
    public int CombatCount { get; init; }
    public int AverageCombatDurationSeconds { get; init; }
    public decimal InsurancePayoutTotal { get; init; }
}
