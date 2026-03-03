using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class ServicesEventProjectorTests
{
    [Fact]
    public void Build_ProjectsTopInfluenceAgents_AsServiceEvents()
    {
        var now = new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc);
        var agents = new[]
        {
            new NpcAgentApiDto { Name = "A", Archetype = "Pirate Syndicate", CurrentGoal = "Raid", Wealth = 2000m, FleetSize = 2, InfluenceScore = 0.5f },
            new NpcAgentApiDto { Name = "B", Archetype = "Trade Conglomerate", CurrentGoal = "Contract", Wealth = 12000m, FleetSize = 3, InfluenceScore = 0.9f }
        };

        var events = ServicesEventProjector.Build(agents, now);

        Assert.Equal(2, events.Count);
        Assert.All(events, static entry => Assert.Equal("Services", entry.Category));
        Assert.Equal("Trade Conglomerate contract: B", events[0].Title);
        Assert.Equal(now, events[0].OccurredAtUtc);
    }
}
