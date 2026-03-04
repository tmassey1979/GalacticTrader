using GalacticTrader.ClientSdk.Shell;

namespace GalacticTrader.ClientSdk.Dashboard;

public sealed record DashboardActionCard(
    DashboardActionType ActionType,
    string Title,
    string Detail,
    GameplayModuleId TargetModule,
    int Priority);
