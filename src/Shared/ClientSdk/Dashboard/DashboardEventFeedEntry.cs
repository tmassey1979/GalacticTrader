namespace GalacticTrader.ClientSdk.Dashboard;

public sealed record DashboardEventFeedEntry(
    DateTime OccurredAtUtc,
    string Category,
    string Title,
    string Detail);
