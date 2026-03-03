using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Realtime;

public static class DashboardRealtimeMessageProjector
{
    public static DashboardRealtimeProjectionResult ApplySnapshot(
        IReadOnlyList<EventFeedEntry> existingEvents,
        DashboardRealtimeSnapshotApiDto snapshot,
        int maxEvents = 200)
    {
        var incomingEvents = snapshot.Events.Select(static entry => new EventFeedEntry
        {
            OccurredAtUtc = entry.OccurredAtUtc.ToUniversalTime(),
            Category = entry.Category,
            Title = entry.Title,
            Detail = entry.Detail
        });

        var mergedEvents = MergeEvents(existingEvents, incomingEvents, maxEvents);
        return new DashboardRealtimeProjectionResult
        {
            Metrics = new StatusMetricSnapshot
            {
                LiquidCredits = snapshot.Metrics.LiquidCredits,
                ReputationScore = snapshot.Metrics.ReputationScore,
                FleetStrength = snapshot.Metrics.FleetStrength,
                ActiveRoutes = snapshot.Metrics.ActiveRoutes,
                AlertCount = snapshot.Metrics.AlertCount
            },
            Events = mergedEvents
        };
    }

    public static IReadOnlyList<EventFeedEntry> AppendEvent(
        IReadOnlyList<EventFeedEntry> existingEvents,
        EventFeedEntry incoming,
        int maxEvents = 200)
    {
        return MergeEvents(existingEvents, [incoming], maxEvents);
    }

    private static IReadOnlyList<EventFeedEntry> MergeEvents(
        IReadOnlyList<EventFeedEntry> existingEvents,
        IEnumerable<EventFeedEntry> incomingEvents,
        int maxEvents)
    {
        var merged = existingEvents
            .Concat(incomingEvents)
            .GroupBy(BuildKey)
            .Select(static group => group.First())
            .OrderByDescending(static entry => entry.OccurredAtUtc)
            .Take(Math.Max(1, maxEvents))
            .ToArray();

        return merged;
    }

    private static string BuildKey(EventFeedEntry entry)
    {
        return $"{entry.Category}|{entry.Title}|{entry.Detail}|{entry.OccurredAtUtc.Ticks}";
    }
}
