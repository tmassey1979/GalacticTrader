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
        var recentTradeVolume = transactions.Take(10).Sum(static transaction => transaction.TotalPrice);
        var highestReputation = standings.Count > 0
            ? standings.Max(static standing => standing.ReputationScore)
            : 0;
        var accessibleFactions = standings.Count(static standing => standing.HasAccess);
        var highRiskRoutes = scene.Routes.Count(static route => route.IsHighRisk);
        var activeReports = reports.Count(static report => !report.IsExpired);
        var alerts = ThreatAlertRanker.Build(dangerousRoutes, reports).Count;

        return new DashboardSummarySnapshot
        {
            LiquidCredits = credits,
            RecentTradeVolume = recentTradeVolume,
            ShipCount = ships.Count,
            FleetStrength = escortSummary?.FleetStrength ?? 0,
            HighestReputation = highestReputation,
            AccessibleFactions = accessibleFactions,
            TotalRoutes = scene.Routes.Count,
            HighRiskRoutes = highRiskRoutes,
            ThreatAlerts = alerts,
            IntelligenceReports = activeReports
        };
    }
}
