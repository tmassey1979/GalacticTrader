using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class AnalyticsSnapshotBuilder
{
    public static AnalyticsSnapshot Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        IReadOnlyList<CombatLogApiDto> combatLogs)
    {
        var tradeCount = transactions.Count;
        var revenueVolume = transactions.Sum(static transaction => transaction.TotalPrice);
        var averageTradeSize = tradeCount > 0 ? revenueVolume / tradeCount : 0m;

        var combatCount = combatLogs.Count;
        var averageDuration = combatCount > 0
            ? (int)Math.Round(combatLogs.Average(static log => log.DurationSeconds))
            : 0;
        var insuranceTotal = combatLogs.Sum(static log => log.InsurancePayout);

        return new AnalyticsSnapshot
        {
            RevenueVolume = revenueVolume,
            TradeCount = tradeCount,
            AverageTradeSize = averageTradeSize,
            CombatCount = combatCount,
            AverageCombatDurationSeconds = averageDuration,
            InsurancePayoutTotal = insuranceTotal
        };
    }
}
