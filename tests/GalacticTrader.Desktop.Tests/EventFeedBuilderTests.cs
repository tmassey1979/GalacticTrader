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

        var events = EventFeedBuilder.Build(transactions, combatLogs, reports, capturedAt);

        Assert.Equal(3, events.Count);
        Assert.Equal("Trade", events[0].Category);
        Assert.DoesNotContain(events, static entry => entry.Title.Contains("OldSignal", StringComparison.Ordinal));
    }
}
