using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class AnalyticsLeaderboardSnapshotBuilderTests
{
    [Fact]
    public void Build_FormatsTopLeadersByCategory()
    {
        var wealth = new[]
        {
            new LeaderboardEntryApiDto { Rank = 2, Username = "other", Score = 100m },
            new LeaderboardEntryApiDto { Rank = 1, Username = "nova", Score = 300m }
        };

        var trade = new[]
        {
            new LeaderboardEntryApiDto { Rank = 1, Username = "pilot", Score = 220m }
        };

        var combat = new[]
        {
            new LeaderboardEntryApiDto { Rank = 1, Username = "ace", Score = 145m }
        };

        var reputation = new[]
        {
            new LeaderboardEntryApiDto { Rank = 1, Username = "vex", Score = 88m }
        };

        var snapshot = AnalyticsLeaderboardSnapshotBuilder.Build(wealth, trade, combat, reputation);

        Assert.Equal("nova (300.0)", snapshot.WealthLeader);
        Assert.Equal("pilot (220.0)", snapshot.TradeLeader);
        Assert.Equal("ace (145.0)", snapshot.CombatLeader);
        Assert.Equal("vex (88.0)", snapshot.ReputationLeader);
    }

    [Fact]
    public void Build_ReturnsNaWhenCategoryIsEmpty()
    {
        var snapshot = AnalyticsLeaderboardSnapshotBuilder.Build([], [], [], []);

        Assert.Equal("n/a", snapshot.WealthLeader);
        Assert.Equal("n/a", snapshot.TradeLeader);
        Assert.Equal("n/a", snapshot.CombatLeader);
        Assert.Equal("n/a", snapshot.ReputationLeader);
    }
}
