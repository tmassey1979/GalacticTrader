using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Intel;
using GalacticTrader.Desktop.Starmap;

namespace GalacticTrader.Desktop.Dashboard;

public static class DashboardSummaryBuilder
{
    public static DashboardSummarySnapshot Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        IReadOnlyList<ShipApiDto> ships,
        EscortSummaryApiDto? escortSummary,
        IReadOnlyList<PlayerFactionStandingApiDto> standings,
        IReadOnlyList<RouteApiDto> dangerousRoutes,
        IReadOnlyList<IntelligenceReportApiDto> reports,
        StarmapScene scene)
    {
        var credits = transactions.Count > 0
            ? transactions[0].RemainingPlayerCredits
            : 0m;
        var netWorth = credits + ships.Sum(static ship => ship.CurrentValue);
        var recentTradeVolume = transactions.Take(10).Sum(static transaction => transaction.TotalPrice);
        var cashFlowTrend = transactions
            .Take(8)
            .Select(static transaction => transaction.RemainingPlayerCredits)
            .Reverse()
            .ToArray();
        var highestReputation = standings.Count > 0
            ? standings.Max(static standing => standing.ReputationScore)
            : 0;
        var accessibleFactions = standings.Count(static standing => standing.HasAccess);
        var highRiskRoutes = scene.Routes.Count(static route => route.IsHighRisk);
        var activeReports = reports.Count(static report => !report.IsExpired);
        var alerts = ThreatAlertRanker.Build(dangerousRoutes, reports).Count;
        var totalRoutes = scene.Routes.Count;
        var assetLiquidityRatio = netWorth <= 0m
            ? 0m
            : Math.Round((credits / netWorth) * 100m, 1);
        var cashFlowIndex = credits <= 0m
            ? 0m
            : Math.Round(Math.Clamp((recentTradeVolume / credits) * 100m, 0m, 100m), 1);
        var fleetRiskExposure = totalRoutes <= 0
            ? 0m
            : Math.Round((highRiskRoutes / (decimal)totalRoutes) * 100m, 1);
        var reputationInfluence = Math.Round(Math.Clamp(highestReputation + (accessibleFactions * 5m), 0m, 100m), 1);
        var revenuePerRoute = totalRoutes <= 0
            ? 0m
            : Math.Round(recentTradeVolume / totalRoutes, 2);
        var interferenceProbability = Math.Round(Math.Clamp(fleetRiskExposure + (alerts * 2m), 0m, 100m), 1);

        return new DashboardSummarySnapshot
        {
            LiquidCredits = credits,
            NetWorth = netWorth,
            AssetLiquidityRatio = assetLiquidityRatio,
            CashFlowIndex = cashFlowIndex,
            RecentTradeVolume = recentTradeVolume,
            CashFlowTrend = cashFlowTrend,
            ShipCount = ships.Count,
            FleetStrength = escortSummary?.FleetStrength ?? 0,
            FleetRiskExposure = fleetRiskExposure,
            HighestReputation = highestReputation,
            AccessibleFactions = accessibleFactions,
            ReputationInfluenceIndex = reputationInfluence,
            TotalRoutes = totalRoutes,
            HighRiskRoutes = highRiskRoutes,
            RevenuePerRoute = revenuePerRoute,
            InterferenceProbability = interferenceProbability,
            ThreatAlerts = alerts,
            IntelligenceReports = activeReports
        };
    }
}
