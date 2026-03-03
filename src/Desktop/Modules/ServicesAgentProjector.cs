using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class ServicesAgentProjector
{
    public static IReadOnlyList<ServicesAgentDisplayRow> Build(
        IReadOnlyList<NpcAgentApiDto> agents,
        IReadOnlySet<Guid> blacklistedAgentIds)
    {
        return agents
            .Where(agent => !blacklistedAgentIds.Contains(agent.Id))
            .Select(agent => new ServicesAgentDisplayRow
            {
                AgentId = agent.Id,
                Name = agent.Name,
                Archetype = agent.Archetype,
                Wealth = agent.Wealth,
                FleetSize = agent.FleetSize,
                InfluenceScore = agent.InfluenceScore,
                AggressionIndex = ComputeAggressionIndex(agent),
                StrategyBias = ResolveStrategyBias(agent),
                CurrentGoal = agent.CurrentGoal
            })
            .OrderByDescending(static row => row.InfluenceScore)
            .ThenByDescending(static row => row.AggressionIndex)
            .ToArray();
    }

    private static int ComputeAggressionIndex(NpcAgentApiDto agent)
    {
        var score = agent.RiskTolerance * 100f
            + agent.InfluenceScore * 15f
            + agent.FleetSize * 6f
            - (agent.ReputationScore * 0.1f);
        return (int)Math.Clamp(Math.Round(score), 0d, 100d);
    }

    private static string ResolveStrategyBias(NpcAgentApiDto agent)
    {
        if (agent.RiskTolerance >= 0.7f && agent.FleetSize >= 3)
        {
            return "Aggressive Expansion";
        }

        if (agent.Wealth >= 50000m && agent.RiskTolerance <= 0.4f)
        {
            return "Defensive Capital";
        }

        return "Balanced Opportunist";
    }
}
