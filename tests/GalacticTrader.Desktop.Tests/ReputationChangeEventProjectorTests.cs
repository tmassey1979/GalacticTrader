using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class ReputationChangeEventProjectorTests
{
    [Fact]
    public void Build_ProjectsOnlyNotableStandingEntries()
    {
        var capturedAt = new DateTime(2026, 3, 3, 2, 0, 0, DateTimeKind.Utc);
        var standings = new[]
        {
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ReputationScore = 20,
                HasAccess = true,
                TradingDiscount = 0.02m
            },
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ReputationScore = 61,
                Tier = "Exalted",
                HasAccess = true,
                TradingDiscount = 0.11m
            },
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                ReputationScore = -48,
                Tier = "Hostile",
                HasAccess = false,
                TradingDiscount = 0m
            }
        };

        var events = ReputationChangeEventProjector.Build(standings, capturedAt);

        Assert.Equal(2, events.Count);
        Assert.All(events, static entry => Assert.Equal("Reputation", entry.Category));
        Assert.DoesNotContain(events, static entry => entry.Title.Contains("aaaaaaaa", StringComparison.Ordinal));
    }
}
