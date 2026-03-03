using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ServicesArchetypeDistributionBuilderTests
{
    [Fact]
    public void Build_GroupsArchetypesAndComputesShares()
    {
        var rows = new[]
        {
            BuildRow("Trade Conglomerate"),
            BuildRow("Trade Conglomerate"),
            BuildRow("Pirate Syndicate"),
            BuildRow("Corporate Protector")
        };

        var result = ServicesArchetypeDistributionBuilder.Build(rows);

        Assert.Equal(3, result.Count);
        Assert.Equal("Trade Conglomerate", result[0].Archetype);
        Assert.Equal(2, result[0].AgentCount);
        Assert.Equal(50.0, result[0].SharePercent);
        Assert.Equal("2 (50.0%)", result[0].ShareSummary);
    }

    [Fact]
    public void Build_NormalizesMissingArchetypeToUnknown()
    {
        var rows = new[]
        {
            BuildRow(""),
            BuildRow("  "),
            BuildRow("Corporate Protector")
        };

        var result = ServicesArchetypeDistributionBuilder.Build(rows);

        Assert.Equal("Unknown", result[0].Archetype);
        Assert.Equal(2, result[0].AgentCount);
    }

    private static ServicesAgentDisplayRow BuildRow(string archetype)
    {
        return new ServicesAgentDisplayRow
        {
            AgentId = Guid.NewGuid(),
            Name = "agent",
            Archetype = archetype,
            StrategyBias = "Balanced Opportunist",
            WealthModel = "Steady Compounder",
            PublicStanding = "Recognized",
            CurrentGoal = "Trade"
        };
    }
}
