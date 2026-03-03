using GalacticTrader.Services.Combat;
using GalacticTrader.Services.Fleet;
using GalacticTrader.Services.Market;
using GalacticTrader.Services.Navigation;
using GalacticTrader.Services.Reputation;
using GalacticTrader.Services.Strategic;

namespace GalacticTrader.Services.Realtime;

public static class DashboardRealtimeSnapshotProjection
{
    public static DashboardRealtimeSnapshotDto Build(
        IReadOnlyList<TradeExecutionResult> transactions,
        IReadOnlyList<PlayerFactionStandingDto> standings,
        EscortSummaryDto? escortSummary,
        IReadOnlyList<ShipDto> ships,
        IReadOnlyList<RouteDto> routes,
        IReadOnlyList<RouteDto> dangerousRoutes,
        IReadOnlyList<IntelligenceReportDto> intelligenceReports,
        IReadOnlyList<CombatLogDto> combatLogs,
        DateTime capturedAtUtc)
    {
        var threatSeverities = dangerousRoutes
            .Select(static route => route.BaseRiskScore)
            .Concat(intelligenceReports.Where(static report => !report.IsExpired)
                .Select(static report => Math.Clamp(report.ConfidenceScore * 100f, 0f, 100f)))
            .ToArray();

        var alertCount = threatSeverities
            .OrderByDescending(static severity => severity)
            .Take(12)
            .Count();

        var credits = transactions.Count > 0 ? transactions[0].RemainingPlayerCredits : 0m;
        var netWorth = credits + ships.Sum(static ship => ship.CurrentValue);
        var reputationScore = standings.Count > 0 ? standings.Max(static standing => standing.ReputationScore) : 0;
        var fleetStrength = escortSummary?.FleetStrength ?? 0;
        var averageTrade = transactions.Count > 0 ? transactions.Average(static transaction => transaction.TotalPrice) : 0m;
        var averageThreat = threatSeverities.Length > 0 ? threatSeverities.Average() : 0d;
        var globalEconomicIndex = Math.Clamp(
            100m + (averageTrade / 100m) + (reputationScore / 2m) - ((decimal)averageThreat / 2m),
            0m,
            200m);

        var metrics = new DashboardRealtimeMetricsDto
        {
            LiquidCredits = credits,
            NetWorth = Math.Round(netWorth, 2),
            ReputationScore = reputationScore,
            FleetStrength = fleetStrength,
            ProtectionStatus = ResolveProtectionStatus(fleetStrength),
            ActiveRoutes = routes.Count,
            AlertCount = alertCount,
            GlobalEconomicIndex = Math.Round(globalEconomicIndex, 1)
        };

        var tradeEvents = transactions.Select(transaction => new DashboardRealtimeEventDto
        {
            OccurredAtUtc = capturedAtUtc,
            Category = "Trade",
            Title = $"{ToActionLabel(transaction.ActionType)} x{transaction.Quantity}",
            Detail = $"Tariff {transaction.TariffAmount:N2} | Total {transaction.TotalPrice:N2} | {transaction.Status}"
        });

        var combatEvents = combatLogs.Select(log => new DashboardRealtimeEventDto
        {
            OccurredAtUtc = log.BattleEndedAt.ToUniversalTime(),
            Category = "Combat",
            Title = log.BattleOutcome,
            Detail = $"Duration {log.DurationSeconds}s | Ticks {log.TotalTicks} | Insurance {log.InsurancePayout:N2}"
        });

        var intelEvents = intelligenceReports
            .Where(static report => !report.IsExpired)
            .Select(report => new DashboardRealtimeEventDto
            {
                OccurredAtUtc = report.DetectedAt.ToUniversalTime(),
                Category = "Intel",
                Title = $"{report.SignalType} @ {report.SectorName}",
                Detail = $"{report.Payload} | Confidence {report.ConfidenceScore:P1}"
            });

        var events = tradeEvents
            .Concat(combatEvents)
            .Concat(intelEvents)
            .OrderByDescending(static entry => entry.OccurredAtUtc)
            .Take(200)
            .ToArray();

        return new DashboardRealtimeSnapshotDto
        {
            CapturedAtUtc = capturedAtUtc,
            Metrics = metrics,
            Events = events
        };
    }

    private static string ToActionLabel(TradeActionType actionType)
    {
        return actionType switch
        {
            TradeActionType.Buy => "Buy",
            TradeActionType.Sell => "Sell",
            _ => actionType.ToString()
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
