using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ReputationInfluenceZoneBuilderTests
{
    [Fact]
    public void Build_ProjectsZoneCountsFromStandings()
    {
        var standings = new[]
        {
            new PlayerFactionStandingApiDto { ReputationScore = 90 },
            new PlayerFactionStandingApiDto { ReputationScore = 65 },
            new PlayerFactionStandingApiDto { ReputationScore = 15 },
            new PlayerFactionStandingApiDto { ReputationScore = -20 }
        };

        var zones = ReputationInfluenceZoneBuilder.Build(standings);

        Assert.Equal(4, zones.Count);
        Assert.Equal("Stronghold Zones: 1", zones[0]);
        Assert.Equal("Contested Zones: 1", zones[1]);
        Assert.Equal("Neutral Zones: 1", zones[2]);
        Assert.Equal("Hostile Zones: 1", zones[3]);
    }

    [Fact]
    public void Build_ReturnsFallback_WhenNoStandings()
    {
        var zones = ReputationInfluenceZoneBuilder.Build([]);

        Assert.Single(zones);
        Assert.Equal("No influence zones available.", zones[0]);
    }
}
