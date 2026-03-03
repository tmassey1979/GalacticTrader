using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Trading;

public static class NpcCompetitorPresenceProjector
{
    public static IReadOnlyList<NpcCompetitorDisplayRow> Build(
        IReadOnlyList<NpcAgentApiDto> agents,
        int maxRows = 6)
    {
        return agents
            .Select(static agent => new NpcCompetitorDisplayRow
            {
                Name = agent.Name,
                Archetype = agent.Archetype,
                FleetSize = agent.FleetSize,
                InfluenceScore = agent.InfluenceScore,
                PresenceScore = ComputePresenceScore(agent)
            })
            .OrderByDescending(static row => row.PresenceScore)
            .ThenBy(static row => row.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxRows))
            .ToArray();
    }

    private static float ComputePresenceScore(NpcAgentApiDto agent)
    {
        var score =
            (agent.InfluenceScore * 0.65f) +
            (agent.FleetSize * 4f) +
            (agent.RiskTolerance * 18f);
        return Math.Clamp(score, 0f, 100f);
    }
}
