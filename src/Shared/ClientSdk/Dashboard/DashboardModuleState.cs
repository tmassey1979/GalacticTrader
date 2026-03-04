namespace GalacticTrader.ClientSdk.Dashboard;

public sealed record DashboardModuleState(
    DashboardActionBoard Board,
    IReadOnlyList<DashboardEventFeedEntry> EventFeed);
