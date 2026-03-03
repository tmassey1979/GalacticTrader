using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class EventFeedBuilderTests
{
    [Fact]
    public void Build_MergesTradeCombatAndIntel_AndSkipsExpiredIntel()
    {
        var capturedAt = new DateTime(2026, 3, 2, 18, 0, 0, DateTimeKind.Utc);
        var transactions = new[]
        {
            new TradeExecutionResultApiDto
            {
                ActionType = 0,
                Quantity = 4,
                TariffAmount = 12.5m,
                TotalPrice = 210m,
                Status = "filled"
            }
        };

        var combatLogs = new[]
        {
            new CombatLogApiDto
            {
                BattleOutcome = "Victory",
                BattleEndedAt = capturedAt.AddMinutes(-10),
                DurationSeconds = 45,
                TotalTicks = 6,
                InsurancePayout = 0m
            }
        };

        var reports = new[]
        {
            new IntelligenceReportApiDto
            {
                SignalType = "PirateFlux",
                SectorName = "Kappa",
                Payload = "raiders massing",
                ConfidenceScore = 0.88f,
                DetectedAt = capturedAt.AddMinutes(-20),
                IsExpired = false
            },
            new IntelligenceReportApiDto
            {
                SignalType = "OldSignal",
                SectorName = "Gamma",
                Payload = "stale",
                ConfidenceScore = 0.99f,
                DetectedAt = capturedAt.AddHours(-2),
                IsExpired = true
            }
        };

        var territory = new[]
        {
            new TerritoryDominanceApiDto
            {
                FactionName = "Orion Syndicate",
                ControlledSectorCount = 5,
                DominanceScore = 51f,
                WarMomentumScore = 42f,
                UpdatedAt = capturedAt.AddMinutes(-5)
            },
            new TerritoryDominanceApiDto
            {
                FactionName = "Stable League",
                ControlledSectorCount = 8,
                DominanceScore = 79f,
                WarMomentumScore = 12f,
                UpdatedAt = capturedAt.AddMinutes(-5)
            }
        };

        var events = EventFeedBuilder.Build(transactions, combatLogs, reports, territory, capturedAt);

        Assert.Equal(4, events.Count);
        Assert.Equal("Trade", events[0].Category);
        Assert.Contains(events, static entry => entry.Category == "Territory" && entry.Title.Contains("Orion Syndicate", StringComparison.Ordinal));
        Assert.DoesNotContain(events, static entry => entry.Title.Contains("OldSignal", StringComparison.Ordinal));
        Assert.DoesNotContain(events, static entry => entry.Title.Contains("Stable League", StringComparison.Ordinal));
    }
}
