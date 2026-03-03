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
        StarmapScene scene)
    {
        var credits = transactions.Count > 0
            ? transactions[0].RemainingPlayerCredits
            : 0m;
        var reputationScore = standings.Count > 0
            ? standings.Max(static standing => standing.ReputationScore)
            : 0;

        return new StatusMetricSnapshot
        {
            LiquidCredits = credits,
            ReputationScore = reputationScore,
            FleetStrength = escortSummary?.FleetStrength ?? 0,
            ActiveRoutes = scene.Routes.Count,
            AlertCount = threats.Count
        };
    }
}
