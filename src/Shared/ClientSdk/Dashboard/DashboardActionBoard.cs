namespace GalacticTrader.ClientSdk.Dashboard;

public sealed record DashboardActionBoard(
    DashboardSnapshot Snapshot,
    IReadOnlyList<DashboardActionCard> Actions);
