using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class ServicesEventProjector
{
    public static IReadOnlyList<EventFeedEntry> Build(
        IReadOnlyList<NpcAgentApiDto> agents,
        DateTime fallbackAtUtc)
    {
        return agents
            .OrderByDescending(static agent => agent.InfluenceScore)
            .Take(6)
            .Select(agent => new EventFeedEntry
            {
                OccurredAtUtc = fallbackAtUtc,
                Category = "Services",
                Title = $"{agent.Archetype} contract: {agent.Name}",
                Detail = $"Goal {agent.CurrentGoal} | Wealth {agent.Wealth:N0} | Fleet {agent.FleetSize}"
            })
            .ToArray();
    }
}
