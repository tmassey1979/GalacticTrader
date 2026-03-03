using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ServicesAgentProjectorTests
{
    [Fact]
    public void Build_FiltersBlacklistedAgents_AndOrdersByInfluence()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var thirdId = Guid.NewGuid();

        var agents = new[]
        {
            new NpcAgentApiDto { Id = firstId, Name = "A", Archetype = "Trader", InfluenceScore = 0.4f, RiskTolerance = 0.3f, FleetSize = 1, ReputationScore = 30, Wealth = 1000m, CurrentGoal = "Trade" },
            new NpcAgentApiDto { Id = secondId, Name = "B", Archetype = "Pirate", InfluenceScore = 0.9f, RiskTolerance = 0.8f, FleetSize = 4, ReputationScore = -20, Wealth = 8000m, CurrentGoal = "Raid" },
            new NpcAgentApiDto { Id = thirdId, Name = "C", Archetype = "Corp", InfluenceScore = 0.7f, RiskTolerance = 0.2f, FleetSize = 2, ReputationScore = 55, Wealth = 75000m, CurrentGoal = "Expand" }
        };

        var rows = ServicesAgentProjector.Build(agents, new HashSet<Guid> { secondId });

        Assert.Equal(2, rows.Count);
        Assert.Equal("C", rows[0].Name);
        Assert.Equal("Recognized", rows[0].PublicStanding);
        Assert.Equal("Steady Compounder", rows[0].WealthModel);
        Assert.Equal("A", rows[1].Name);
        Assert.Equal("Recognized", rows[1].PublicStanding);
    }
}
