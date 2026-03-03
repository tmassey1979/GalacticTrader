using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class AnalyticsSnapshotBuilder
{
    public static AnalyticsSnapshot Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        IReadOnlyList<CombatLogApiDto> combatLogs,
        IReadOnlyList<ShipApiDto> ships,
        IReadOnlyList<TopTraderInsightApiDto> topTraders,
        IReadOnlyList<PlayerFactionStandingApiDto> factionStandings,
        string playerUsername)
    {
        var tradeCount = transactions.Count;
        var revenueVolume = transactions.Sum(static transaction => transaction.TotalPrice);
        var averageTradeSize = tradeCount > 0 ? revenueVolume / tradeCount : 0m;

        var combatCount = combatLogs.Count;
        var averageDuration = combatCount > 0
            ? (int)Math.Round(combatLogs.Average(static log => log.DurationSeconds))
            : 0;
        var insuranceTotal = combatLogs.Sum(static log => log.InsurancePayout);
        var riskExposure = 1m + (combatCount * 0.15m);
        var riskAdjustedReturn = (revenueVolume - insuranceTotal) / riskExposure;
        var battleToProfitRatio = revenueVolume > 0m
            ? Math.Round(combatCount / (revenueVolume / 10_000m), 4)
            : 0m;
        var shipCount = ships.Count;
        var roiPerShip = shipCount > 0
            ? (revenueVolume - insuranceTotal) / shipCount
            : 0m;
        var marketSharePercent = ComputeMarketSharePercent(topTraders, playerUsername);
        var systemInfluencePercent = ComputeSystemInfluencePercent(factionStandings);

        return new AnalyticsSnapshot
        {
            RevenueVolume = revenueVolume,
            TradeCount = tradeCount,
            AverageTradeSize = averageTradeSize,
            CombatCount = combatCount,
            AverageCombatDurationSeconds = averageDuration,
            InsurancePayoutTotal = insuranceTotal,
            RiskAdjustedReturn = Math.Round(riskAdjustedReturn, 2),
            BattleToProfitRatio = battleToProfitRatio,
            RoiPerShip = Math.Round(roiPerShip, 2),
            MarketSharePercent = marketSharePercent,
            SystemInfluencePercent = systemInfluencePercent
        };
    }

    private static decimal ComputeMarketSharePercent(
        IReadOnlyList<TopTraderInsightApiDto> topTraders,
        string playerUsername)
    {
        if (topTraders.Count == 0 || string.IsNullOrWhiteSpace(playerUsername))
        {
            return 0m;
        }

        var totalVolume = topTraders.Sum(static trader => trader.TradeVolume);
        if (totalVolume <= 0m)
        {
            return 0m;
        }

        var playerVolume = topTraders
            .Where(trader => string.Equals(trader.Username, playerUsername, StringComparison.OrdinalIgnoreCase))
            .Sum(static trader => trader.TradeVolume);
        return Math.Round((playerVolume / totalVolume) * 100m, 2);
    }

    private static decimal ComputeSystemInfluencePercent(IReadOnlyList<PlayerFactionStandingApiDto> standings)
    {
        if (standings.Count == 0)
        {
            return 0m;
        }

        var averageReputation = (decimal)standings.Average(static standing => standing.ReputationScore);
        var normalizedReputation = Math.Clamp((averageReputation + 100m) / 2m, 0m, 100m);
        var accessShare = standings.Count(static standing => standing.HasAccess) / (decimal)standings.Count * 100m;
        var weighted = (normalizedReputation * 0.6m) + (accessShare * 0.4m);
        return Math.Round(Math.Clamp(weighted, 0m, 100m), 2);
    }
}
