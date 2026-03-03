using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class DashboardGlobalMetricsBuilder
{
    public static DashboardGlobalMetricsSnapshot Build(GlobalMetricsSummaryApiDto summary)
    {
        return new DashboardGlobalMetricsSnapshot
        {
            TotalUsers = summary.TotalUsers,
            ActivePlayers24h = summary.ActivePlayers24h,
            AvgBattlesPerHour = Math.Round(summary.AvgBattlesPerHour, 2, MidpointRounding.AwayFromZero),
            EconomicStabilityIndex = Math.Round(summary.EconomicStabilityIndex, 1, MidpointRounding.AwayFromZero),
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
}
