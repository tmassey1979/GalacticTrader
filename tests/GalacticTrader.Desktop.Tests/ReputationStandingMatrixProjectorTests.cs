using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ReputationStandingMatrixProjectorTests
{
    [Fact]
    public void Build_ProjectsNormalizedStandingIntensity()
    {
        var standings = new[]
        {
            new PlayerFactionStandingApiDto { FactionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ReputationScore = 80, Tier = "Exalted" },
            new PlayerFactionStandingApiDto { FactionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), ReputationScore = -20, Tier = "Suspicious" }
        };

        var rows = ReputationStandingMatrixProjector.Build(standings);

        Assert.Equal(2, rows.Count);
        Assert.Equal("aaaaaaaa", rows[0].FactionId);
        Assert.Equal(90d, rows[0].NormalizedScorePercent);
        Assert.Equal("80 [Exalted]", rows[0].ScoreSummary);

        Assert.Equal("bbbbbbbb", rows[1].FactionId);
        Assert.Equal(40d, rows[1].NormalizedScorePercent);
    }
}
