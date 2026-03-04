using GalacticTrader.ClientSdk.Shell;

namespace GalacticTrader.ClientSdk.Dashboard;

public static class DashboardActionPlanner
{
    public static IReadOnlyList<DashboardActionCard> Build(DashboardSnapshot snapshot)
    {
        var actions = new List<DashboardActionCard>();

        if (snapshot.ShipCount == 0)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.AcquireShip,
                "Acquire Your First Ship",
                "Open Fleet and purchase or claim a starter ship to unlock trade and route actions.",
                GameplayModuleId.Fleet,
                Priority: 100));
        }

        if (snapshot.AvailableCredits < 25_000m)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.ImproveLiquidity,
                "Build Liquidity",
                "Run short trade loops to stabilize available credits before high-risk expansion.",
                GameplayModuleId.Trading,
                Priority: 90));
        }

        if (snapshot.DangerousRouteCount > 0 && snapshot.EscortStrength < Math.Max(1, snapshot.FleetStrength / 2))
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.ReinforceEscort,
                "Reinforce Escort Coverage",
                "Dangerous routes are active and escort strength is below safe range.",
                GameplayModuleId.Fleet,
                Priority: 85));
        }

        if (snapshot.DangerousRouteCount > 0)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.ReviewDangerousRoutes,
                "Review Dangerous Routes",
                $"There are {snapshot.DangerousRouteCount} high-risk routes above threshold.",
                GameplayModuleId.Routes,
                Priority: 80));
        }

        if (snapshot.ActiveIntelligenceCount > 0)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.ReviewIntelligence,
                "Review Intelligence Alerts",
                $"You have {snapshot.ActiveIntelligenceCount} active intelligence reports that may affect routing and combat.",
                GameplayModuleId.Intel,
                Priority: 75));
        }

        if (snapshot.ReputationScore < 0)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.RepairReputation,
                "Repair Faction Standing",
                "Negative standing is reducing access and trade flexibility.",
                GameplayModuleId.Reputation,
                Priority: 70));
        }

        if (snapshot.EconomicStabilityIndex < 50m)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.DefensiveTrading,
                "Switch To Defensive Trading",
                "Global economic stability is low. Favor conservative routes and inventory positions.",
                GameplayModuleId.Trading,
                Priority: 65));
        }

        if (actions.Count == 0)
        {
            actions.Add(new DashboardActionCard(
                DashboardActionType.ExpandTradeNetwork,
                "Expand Your Trade Network",
                "Core systems are stable. Add new routes and increase cargo throughput.",
                GameplayModuleId.Routes,
                Priority: 50));
        }

        return actions
            .OrderByDescending(static action => action.Priority)
            .ThenBy(static action => action.ActionType)
            .ToArray();
    }
}
