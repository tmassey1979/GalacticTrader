using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Battles;

namespace GalacticTrader.Desktop.Tests;

public sealed class BattleOutcomeProjectorTests
{
    [Fact]
    public void Project_Victory_YieldsPositiveReputationAndImpact()
    {
        var projection = BattleOutcomeProjector.Project(new CombatLogApiDto
        {
            BattleOutcome = "Victory",
            DurationSeconds = 90,
            TotalTicks = 12,
            InsurancePayout = 600m
        });

        Assert.Equal(2, projection.ReputationDelta);
        Assert.True(projection.EconomicImpactProjection > 0m);
        Assert.Equal("Major damage", projection.DamageReport);
    }

    [Fact]
    public void Project_Defeat_YieldsNegativeReputationAndCriticalDamage()
    {
        var projection = BattleOutcomeProjector.Project(new CombatLogApiDto
        {
            BattleOutcome = "Defeat",
            DurationSeconds = 240,
            TotalTicks = 40,
            InsurancePayout = 5000m
        });

        Assert.Equal(-2, projection.ReputationDelta);
        Assert.True(projection.EconomicImpactProjection < 0m);
        Assert.Equal("Critical losses", projection.DamageReport);
    }
}
