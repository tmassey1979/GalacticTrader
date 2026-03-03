using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ReputationBadgeProjectorTests
{
    [Fact]
    public void Build_MapsKnownTierToBadge()
    {
        var standing = new PlayerFactionStandingApiDto
        {
            Tier = "Exalted"
        };

        var badge = ReputationBadgeProjector.Build(standing);

        Assert.Equal("Paragon", badge.Badge);
        Assert.Equal("#F4C542", badge.AccentHex);
    }

    [Fact]
    public void Build_UsesFallbackForUnknownTier()
    {
        var standing = new PlayerFactionStandingApiDto
        {
            Tier = "CustomTier"
        };

        var badge = ReputationBadgeProjector.Build(standing);

        Assert.Equal("Unknown", badge.Badge);
    }
}
