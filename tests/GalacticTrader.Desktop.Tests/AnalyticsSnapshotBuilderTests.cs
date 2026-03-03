using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class AnalyticsSnapshotBuilderTests
{
    [Fact]
    public void Build_ComputesTradeAndCombatAggregates()
    {
        var trades = new[]
        {
            new TradeExecutionResultApiDto { TotalPrice = 120m },
            new TradeExecutionResultApiDto { TotalPrice = 180m },
            new TradeExecutionResultApiDto { TotalPrice = 60m }
        };

        var combats = new[]
        {
            new CombatLogApiDto { DurationSeconds = 20, InsurancePayout = 5m },
            new CombatLogApiDto { DurationSeconds = 40, InsurancePayout = 2m }
        };

        var ships = new[]
        {
            new ShipApiDto(),
            new ShipApiDto()
        };
        var topTraders = new[]
        {
            new TopTraderInsightApiDto { Username = "pilot", TradeVolume = 300m },
            new TopTraderInsightApiDto { Username = "other", TradeVolume = 700m }
        };
        var standings = new[]
        {
            new PlayerFactionStandingApiDto { ReputationScore = 80, HasAccess = true },
            new PlayerFactionStandingApiDto { ReputationScore = 20, HasAccess = false }
        };

        var snapshot = AnalyticsSnapshotBuilder.Build(trades, combats, ships, topTraders, standings, "pilot");

        Assert.Equal(360m, snapshot.RevenueVolume);
        Assert.Equal(360m, snapshot.RevenuePerHour);
        Assert.Equal(3, snapshot.TradeCount);
        Assert.Equal(120m, snapshot.AverageTradeSize);
        Assert.Equal(2, snapshot.CombatCount);
        Assert.Equal(30, snapshot.AverageCombatDurationSeconds);
        Assert.Equal(7m, snapshot.InsurancePayoutTotal);
        Assert.Equal(271.54m, snapshot.RiskAdjustedReturn);
        Assert.Equal(55.5556m, snapshot.BattleToProfitRatio);
        Assert.Equal(176.5m, snapshot.RoiPerShip);
        Assert.Equal(30m, snapshot.MarketSharePercent);
        Assert.Equal(65m, snapshot.SystemInfluencePercent);
    }

    [Fact]
    public void Build_ReturnsZeroedAdvancedMetrics_WhenNoReferenceData()
    {
        var snapshot = AnalyticsSnapshotBuilder.Build([], [], [], [], [], "pilot");

        Assert.Equal(0m, snapshot.RevenuePerHour);
        Assert.Equal(0m, snapshot.RiskAdjustedReturn);
        Assert.Equal(0m, snapshot.BattleToProfitRatio);
        Assert.Equal(0m, snapshot.RoiPerShip);
        Assert.Equal(0m, snapshot.MarketSharePercent);
        Assert.Equal(0m, snapshot.SystemInfluencePercent);
    }

    [Fact]
    public void Build_ScalesRevenuePerHourByEstimatedTradeWindow()
    {
        var trades = Enumerable.Range(0, 12)
            .Select(_ => new TradeExecutionResultApiDto { TotalPrice = 100m })
            .ToArray();

        var snapshot = AnalyticsSnapshotBuilder.Build(trades, [], [], [], [], "pilot");

        Assert.Equal(1200m, snapshot.RevenueVolume);
        Assert.Equal(600m, snapshot.RevenuePerHour);
    }
}
