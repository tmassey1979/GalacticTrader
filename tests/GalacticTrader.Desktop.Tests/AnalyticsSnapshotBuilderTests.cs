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

        var snapshot = AnalyticsSnapshotBuilder.Build(trades, combats);

        Assert.Equal(360m, snapshot.RevenueVolume);
        Assert.Equal(3, snapshot.TradeCount);
        Assert.Equal(120m, snapshot.AverageTradeSize);
        Assert.Equal(2, snapshot.CombatCount);
        Assert.Equal(30, snapshot.AverageCombatDurationSeconds);
        Assert.Equal(7m, snapshot.InsurancePayoutTotal);
    }
}
