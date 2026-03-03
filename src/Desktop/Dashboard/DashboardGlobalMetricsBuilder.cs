using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class DashboardGlobalMetricsBuilder
{
    public static DashboardGlobalMetricsSnapshot Build(GlobalMetricsSummaryApiDto summary)
    {
        var avgBattlesPerHour = Math.Round(summary.AvgBattlesPerHour, 2, MidpointRounding.AwayFromZero);
        var economicStabilityIndex = Math.Round(summary.EconomicStabilityIndex, 1, MidpointRounding.AwayFromZero);
        var tradeVolumeIndex = ComputeTradeVolumeIndex(summary.ActivePlayers24h, economicStabilityIndex);
        var factionStabilityIndex = ComputeFactionStabilityIndex(economicStabilityIndex, avgBattlesPerHour);
        var combatIntensityIndex = ComputeCombatIntensityIndex(avgBattlesPerHour);

        return new DashboardGlobalMetricsSnapshot
        {
            TotalUsers = summary.TotalUsers,
            ActivePlayers24h = summary.ActivePlayers24h,
            AvgBattlesPerHour = avgBattlesPerHour,
            EconomicStabilityIndex = economicStabilityIndex,
            TradeVolumeIndex = tradeVolumeIndex,
            FactionStabilityIndex = factionStabilityIndex,
            CombatIntensityIndex = combatIntensityIndex,
            TopReputationPlayerDisplay = BuildPlayerDisplay(summary.TopReputationPlayer),
            TopFinancialPlayerDisplay = BuildPlayerDisplay(summary.TopFinancialPlayer)
        };
    }

    private static string BuildPlayerDisplay(GlobalTopPlayerApiDto player)
    {
        if (string.IsNullOrWhiteSpace(player.Username))
        {
            return "n/a";
        }

        return $"{player.Username} ({player.Score:N1})";
    }

    private static decimal ComputeTradeVolumeIndex(int activePlayers24h, decimal economicStabilityIndex)
    {
        var participationFactor = Math.Clamp(activePlayers24h / 120m, 0m, 1.2m);
        var blended = (economicStabilityIndex * 0.60m) + (participationFactor * 100m * 0.40m);
        return Math.Round(Math.Clamp(blended, 0m, 100m), 1, MidpointRounding.AwayFromZero);
    }

    private static decimal ComputeFactionStabilityIndex(decimal economicStabilityIndex, decimal avgBattlesPerHour)
    {
        var battleDrag = Math.Clamp(avgBattlesPerHour * 3.5m, 0m, 35m);
        var value = economicStabilityIndex - battleDrag + 8m;
        return Math.Round(Math.Clamp(value, 0m, 100m), 1, MidpointRounding.AwayFromZero);
    }

    private static decimal ComputeCombatIntensityIndex(decimal avgBattlesPerHour)
    {
        var value = avgBattlesPerHour * 12m;
        return Math.Round(Math.Clamp(value, 0m, 100m), 1, MidpointRounding.AwayFromZero);
    }
}
