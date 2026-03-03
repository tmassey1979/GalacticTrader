using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class InterceptionEventProjectorTests
{
    [Fact]
    public void Build_ProjectsRetreatAndInsuranceCombatOutcomes()
    {
        var capturedAt = new DateTime(2026, 3, 3, 18, 0, 0, DateTimeKind.Utc);
        var logs = new[]
        {
            new CombatLogApiDto
            {
                BattleOutcome = "Victory",
                BattleEndedAt = capturedAt,
                DurationSeconds = 40,
                InsurancePayout = 0m
            },
            new CombatLogApiDto
            {
                BattleOutcome = "Retreat",
                BattleEndedAt = capturedAt.AddMinutes(-2),
                DurationSeconds = 32,
                InsurancePayout = 0m
            },
            new CombatLogApiDto
            {
                BattleOutcome = "Victory",
                BattleEndedAt = capturedAt.AddMinutes(-4),
                DurationSeconds = 70,
                InsurancePayout = 220m
            }
        };

        var events = InterceptionEventProjector.Build(logs);

        Assert.Equal(2, events.Count);
        Assert.All(events, entry => Assert.Equal("Interception", entry.Category));
    }
}
