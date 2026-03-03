using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class EventFeedBuilder
{
    public static IReadOnlyList<EventFeedEntry> Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        IReadOnlyList<CombatLogApiDto> combatLogs,
        IReadOnlyList<IntelligenceReportApiDto> intelligenceReports,
        DateTime capturedAtUtc)
    {
        var tradeEvents = transactions.Select(transaction => new EventFeedEntry
        {
            OccurredAtUtc = capturedAtUtc,
            Category = "Trade",
            Title = $"{(transaction.ActionType == 0 ? "Buy" : "Sell")} x{transaction.Quantity}",
            Detail = $"Tariff {transaction.TariffAmount:N2} | Total {transaction.TotalPrice:N2} | {transaction.Status}"
        });

        var combatEvents = combatLogs.Select(log => new EventFeedEntry
        {
            OccurredAtUtc = log.BattleEndedAt.ToUniversalTime(),
            Category = "Combat",
            Title = log.BattleOutcome,
            Detail = $"Duration {log.DurationSeconds}s | Ticks {log.TotalTicks} | Insurance {log.InsurancePayout:N2}"
        });

        var intelEvents = intelligenceReports
            .Where(static report => !report.IsExpired)
            .Select(report => new EventFeedEntry
            {
                OccurredAtUtc = report.DetectedAt.ToUniversalTime(),
                Category = "Intel",
                Title = $"{report.SignalType} @ {report.SectorName}",
                Detail = $"{report.Payload} | Confidence {report.ConfidenceScore:P1}"
            });

        return tradeEvents
            .Concat(combatEvents)
            .Concat(intelEvents)
            .OrderByDescending(static entry => entry.OccurredAtUtc)
            .ToArray();
    }
}
