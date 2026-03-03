using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class InterceptionEventProjector
{
    public static IReadOnlyList<EventFeedEntry> Build(IReadOnlyList<CombatLogApiDto> combatLogs)
    {
        return combatLogs
            .Where(IsInterceptionSignal)
            .Select(log => new EventFeedEntry
            {
                OccurredAtUtc = log.BattleEndedAt.ToUniversalTime(),
                Category = "Interception",
                Title = $"Interception {ResolveInterceptionOutcome(log.BattleOutcome)}",
                Detail = $"Outcome {log.BattleOutcome} | Duration {log.DurationSeconds}s | Insurance {log.InsurancePayout:N2}"
            })
            .ToArray();
    }

    private static bool IsInterceptionSignal(CombatLogApiDto log)
    {
        if (log.InsurancePayout > 0m)
        {
            return true;
        }

        var outcome = (log.BattleOutcome ?? string.Empty).Trim().ToLowerInvariant();
        return outcome.Contains("retreat", StringComparison.Ordinal) ||
               outcome.Contains("defeat", StringComparison.Ordinal) ||
               outcome.Contains("loss", StringComparison.Ordinal);
    }

    private static string ResolveInterceptionOutcome(string outcome)
    {
        var normalized = (outcome ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Detected" : normalized;
    }
}
