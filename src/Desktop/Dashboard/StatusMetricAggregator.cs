using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Intel;
using GalacticTrader.Desktop.Starmap;

namespace GalacticTrader.Desktop.Dashboard;

public static class StatusMetricAggregator
{
    public static StatusMetricSnapshot Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        IReadOnlyList<PlayerFactionStandingApiDto> standings,
        EscortSummaryApiDto? escortSummary,
        IReadOnlyList<ThreatAlert> threats,
        IReadOnlyList<ShipApiDto> ships,
        StarmapScene scene)
    {
        var credits = transactions.Count > 0
            ? transactions[0].RemainingPlayerCredits
            : 0m;
        var netWorth = credits + ships.Sum(static ship => ship.CurrentValue);
        var reputationScore = standings.Count > 0
            ? standings.Max(static standing => standing.ReputationScore)
            : 0;
        var protectionStatus = ResolveProtectionStatus(escortSummary?.FleetStrength ?? 0);

        var averageTrade = transactions.Count > 0
            ? transactions.Average(static transaction => transaction.TotalPrice)
            : 0m;
        var averageThreatSeverity = threats.Count > 0
            ? threats.Average(static alert => alert.Severity)
            : 0d;
        var globalEconomicIndex = Math.Clamp(
            100m + (averageTrade / 100m) + (reputationScore / 2m) - ((decimal)averageThreatSeverity / 2m),
            0m,
            200m);

        return new StatusMetricSnapshot
        {
            LiquidCredits = credits,
            NetWorth = Math.Round(netWorth, 2),
            ReputationScore = reputationScore,
            FleetStrength = escortSummary?.FleetStrength ?? 0,
            ProtectionStatus = protectionStatus,
            ActiveRoutes = scene.Routes.Count,
            AlertCount = threats.Count,
            GlobalEconomicIndex = Math.Round(globalEconomicIndex, 1)
        };
    }

    private static string ResolveProtectionStatus(int fleetStrength)
    {
        return fleetStrength switch
        {
            >= 150 => "Fortified",
            >= 80 => "Guarded",
            >= 30 => "Contested",
            > 0 => "Fragile",
            _ => "Unprotected"
        };
    }
}
