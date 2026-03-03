using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ReputationImpactForecastProjectorTests
{
    [Fact]
    public void Build_ProjectsImpactMetricsForOutlawAlignedProfile()
    {
        var standings = new[]
        {
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ReputationScore = 70,
                HasAccess = true,
                TradingDiscount = 0.14m
            },
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ReputationScore = 30,
                HasAccess = true,
                TradingDiscount = 0.04m
            },
            new PlayerFactionStandingApiDto
            {
                FactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                ReputationScore = -40,
                HasAccess = false,
                TradingDiscount = 0m
            }
        };

        var access = new AlignmentAccessApiDto
        {
            Path = "Outlaw",
            CanUseLegalInsurance = false,
            CanAccessBlackMarket = true,
            InsuranceCostModifier = 0.10f
        };

        var projection = ReputationImpactForecastProjector.Build(standings, access);

        Assert.Equal(0.0925m, projection.TradeMarginModifier);
        Assert.Equal(0.165m, projection.ProtectionCostModifier);
        Assert.Equal(0.725m, projection.SmugglingSuccessChance);
        Assert.Equal("Selective (2/3 factions) - Outlaw path", projection.AllianceAccessSummary);
    }

    [Fact]
    public void Build_ClampsExtremeForecastValues()
    {
        var standings = Enumerable.Range(0, 30)
            .Select(_ => new PlayerFactionStandingApiDto
            {
                FactionId = Guid.NewGuid(),
                ReputationScore = -100,
                HasAccess = false,
                TradingDiscount = 0m
            })
            .ToArray();

        var access = new AlignmentAccessApiDto
        {
            Path = "Lawful",
            CanUseLegalInsurance = false,
            CanAccessBlackMarket = false,
            InsuranceCostModifier = 1.20f
        };

        var projection = ReputationImpactForecastProjector.Build(standings, access);

        Assert.Equal(-0.20m, projection.TradeMarginModifier);
        Assert.Equal(0.80m, projection.ProtectionCostModifier);
        Assert.Equal(0.85m, projection.SmugglingSuccessChance);
        Assert.Equal("Limited (0/30 factions) - Lawful path", projection.AllianceAccessSummary);
    }
}
