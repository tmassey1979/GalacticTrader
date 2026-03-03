using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class NpcCompetitorPresenceProjectorTests
{
    [Fact]
    public void Build_ProjectsAndSortsByPresenceScore()
    {
        var agents = new[]
        {
            new NpcAgentApiDto { Name = "Corsair", Archetype = "Pirate", InfluenceScore = 45f, FleetSize = 4, RiskTolerance = 0.9f },
            new NpcAgentApiDto { Name = "Broker", Archetype = "Trader", InfluenceScore = 70f, FleetSize = 3, RiskTolerance = 0.2f },
            new NpcAgentApiDto { Name = "Warden", Archetype = "Protector", InfluenceScore = 55f, FleetSize = 8, RiskTolerance = 0.1f }
        };

        var rows = NpcCompetitorPresenceProjector.Build(agents, maxRows: 3);

        Assert.Equal(3, rows.Count);
        Assert.Equal("Warden", rows[0].Name);
        Assert.InRange(rows[0].PresenceScore, 0f, 100f);
    }

    [Fact]
    public void Build_ClampsPresenceScore()
    {
        var agents = new[]
        {
            new NpcAgentApiDto { Name = "Overlord", Archetype = "Pirate", InfluenceScore = 500f, FleetSize = 100, RiskTolerance = 9f }
        };

        var rows = NpcCompetitorPresenceProjector.Build(agents, maxRows: 1);

        Assert.Equal(100f, rows[0].PresenceScore);
    }
}
