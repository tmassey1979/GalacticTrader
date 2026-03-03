using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ReputationSanctionBonusProjectorTests
{
    [Fact]
    public void Build_ProjectsBonusesAndSanctionsByStanding()
    {
        var standings = new[]
        {
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ReputationScore = 72,
                HasAccess = true,
                TradingDiscount = 0.12m
            },
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ReputationScore = -30,
                HasAccess = true,
                TradingDiscount = 0m
            },
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                ReputationScore = -65,
                HasAccess = false,
                TradingDiscount = 0m
            }
        };

        var rows = ReputationSanctionBonusProjector.Build(standings);

        Assert.Equal(3, rows.Count);
        Assert.Equal("aaaaaaaa", rows[0].FactionId);
        Assert.Equal("Escort subsidy -10%", rows[0].Bonus);
        Assert.Equal("None", rows[0].Sanction);

        Assert.Equal("bbbbbbbb", rows[1].FactionId);
        Assert.Equal("Standard market access", rows[1].Bonus);
        Assert.Equal("Elevated inspection risk", rows[1].Sanction);

        Assert.Equal("cccccccc", rows[2].FactionId);
        Assert.Equal("None", rows[2].Bonus);
        Assert.Equal("Market access restricted", rows[2].Sanction);
    }
}
