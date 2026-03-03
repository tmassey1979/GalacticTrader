using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class TerritoryConflictEventProjectorTests
{
    [Fact]
    public void Build_IncludesConflictSignals_AndSkipsStableDominance()
    {
        var now = new DateTime(2026, 3, 3, 2, 0, 0, DateTimeKind.Utc);
        var rows = new[]
        {
            new TerritoryDominanceApiDto
            {
                FactionName = "A",
                ControlledSectorCount = 3,
                DominanceScore = 55f,
                WarMomentumScore = 30f,
                UpdatedAt = now.AddMinutes(-3)
            },
            new TerritoryDominanceApiDto
            {
                FactionName = "B",
                ControlledSectorCount = 7,
                DominanceScore = 72f,
                WarMomentumScore = 67f,
                UpdatedAt = now.AddMinutes(-2)
            },
            new TerritoryDominanceApiDto
            {
                FactionName = "C",
                ControlledSectorCount = 9,
                DominanceScore = 75f,
                WarMomentumScore = 20f,
                UpdatedAt = now.AddMinutes(-1)
            }
        };

        var events = TerritoryConflictEventProjector.Build(rows, now);

        Assert.Equal(2, events.Count);
        Assert.All(events, static item => Assert.Equal("Territory", item.Category));
        Assert.DoesNotContain(events, static item => item.Title.EndsWith(": C", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_UsesFallbackTimestamp_WhenUpdatedAtMissing()
    {
        var now = new DateTime(2026, 3, 3, 3, 0, 0, DateTimeKind.Utc);
        var rows = new[]
        {
            new TerritoryDominanceApiDto
            {
                FactionName = "Fallback",
                ControlledSectorCount = 4,
                DominanceScore = 45f,
                WarMomentumScore = 10f,
                UpdatedAt = default
            }
        };

        var events = TerritoryConflictEventProjector.Build(rows, now);

        var entry = Assert.Single(events);
        Assert.Equal(now, entry.OccurredAtUtc);
    }
}
