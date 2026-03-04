using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Dashboard;

public static class DashboardRealtimeStateProjector
{
    public static DashboardModuleState ApplySnapshot(
        DashboardActionBoard currentBoard,
        IReadOnlyList<DashboardEventFeedEntry> existingFeed,
        DashboardRealtimeSnapshotApiDto snapshot,
        int maxEvents = 200)
    {
        var mergedFeed = MergeEvents(
            existingFeed,
            snapshot.Events.Select(static @event => new DashboardEventFeedEntry(
                @event.OccurredAtUtc.ToUniversalTime(),
                @event.Category,
                @event.Title,
                @event.Detail)),
            maxEvents);

        var updatedSnapshot = currentBoard.Snapshot with
        {
            AvailableCredits = snapshot.Metrics.LiquidCredits,
            FleetStrength = snapshot.Metrics.FleetStrength,
            ReputationScore = snapshot.Metrics.ReputationScore,
            DangerousRouteCount = Math.Max(currentBoard.Snapshot.DangerousRouteCount, snapshot.Metrics.AlertCount),
            EconomicStabilityIndex = snapshot.Metrics.GlobalEconomicIndex
        };

        var updatedBoard = new DashboardActionBoard(updatedSnapshot, DashboardActionPlanner.Build(updatedSnapshot));
        return new DashboardModuleState(updatedBoard, mergedFeed);
    }

    private static IReadOnlyList<DashboardEventFeedEntry> MergeEvents(
        IReadOnlyList<DashboardEventFeedEntry> existingEvents,
        IEnumerable<DashboardEventFeedEntry> incomingEvents,
        int maxEvents)
    {
        return existingEvents
            .Concat(incomingEvents)
            .GroupBy(BuildKey)
            .Select(static group => group.First())
            .OrderByDescending(static entry => entry.OccurredAtUtc)
            .Take(Math.Max(1, maxEvents))
            .ToArray();
    }

    private static string BuildKey(DashboardEventFeedEntry entry)
    {
        return $"{entry.Category}|{entry.Title}|{entry.Detail}|{entry.OccurredAtUtc.Ticks}";
    }
}
