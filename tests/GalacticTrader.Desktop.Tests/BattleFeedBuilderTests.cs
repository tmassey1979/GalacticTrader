using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Battles;

namespace GalacticTrader.Desktop.Tests;

public sealed class BattleFeedBuilderTests
{
    [Fact]
    public void Build_OrdersByBattleEndDescending_AndFormatsKeys()
    {
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new CombatLogApiDto
            {
                AttackerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                DefenderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                BattleOutcome = "Victory",
                BattleEndedAt = now.AddMinutes(-15),
                DurationSeconds = 30,
                TotalTicks = 4,
                InsurancePayout = 5m
            },
            new CombatLogApiDto
            {
                AttackerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                DefenderId = null,
                BattleOutcome = "Retreat",
                BattleEndedAt = now.AddMinutes(-5),
                DurationSeconds = 12,
                TotalTicks = 2,
                InsurancePayout = 0m
            }
        };

        var rows = BattleFeedBuilder.Build(logs);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Retreat", rows[0].Outcome);
        Assert.Equal("cccccccc", rows[0].Attacker);
        Assert.Equal("-", rows[0].Defender);
        Assert.Equal(-1, rows[0].ReputationDelta);
        Assert.True(rows[0].EconomicImpactProjection < 0m);
        Assert.Equal("Minor damage", rows[0].DamageReport);
    }
}
