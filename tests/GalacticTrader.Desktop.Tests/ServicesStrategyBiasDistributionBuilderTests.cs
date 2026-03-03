using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ServicesStrategyBiasDistributionBuilderTests
{
    [Fact]
    public void Build_GroupsBiasesAndComputesSharePercent()
    {
        var rows = new[]
        {
            BuildRow("Aggressive Expansion"),
            BuildRow("Aggressive Expansion"),
            BuildRow("Balanced Opportunist"),
            BuildRow("Defensive Capital")
        };

        var result = ServicesStrategyBiasDistributionBuilder.Build(rows);

        Assert.Equal(3, result.Count);
        Assert.Equal("Aggressive Expansion", result[0].StrategyBias);
        Assert.Equal(2, result[0].AgentCount);
        Assert.Equal(50.0, result[0].SharePercent);
        Assert.Equal("2 (50.0%)", result[0].ShareSummary);

        Assert.Equal("Balanced Opportunist", result[1].StrategyBias);
        Assert.Equal(1, result[1].AgentCount);
        Assert.Equal(25.0, result[1].SharePercent);
    }

    [Fact]
    public void Build_NormalizesBlankBiasToUnknown()
    {
        var rows = new[]
        {
            BuildRow(""),
            BuildRow("   "),
            BuildRow("Defensive Capital")
        };

        var result = ServicesStrategyBiasDistributionBuilder.Build(rows);

        Assert.Equal(2, result.Count);
        Assert.Equal("Unknown", result[0].StrategyBias);
        Assert.Equal(2, result[0].AgentCount);
        Assert.Equal(66.7, result[0].SharePercent);
    }

    private static ServicesAgentDisplayRow BuildRow(string strategyBias)
    {
        return new ServicesAgentDisplayRow
        {
            AgentId = Guid.NewGuid(),
            Name = "agent",
            Archetype = "Trader",
            StrategyBias = strategyBias,
            WealthModel = "Steady Compounder",
            PublicStanding = "Recognized",
            CurrentGoal = "Trade"
        };
    }
}
