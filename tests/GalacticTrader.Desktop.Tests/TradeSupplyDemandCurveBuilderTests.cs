using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradeSupplyDemandCurveBuilderTests
{
    [Fact]
    public void Build_ComputesDemandSupplyRatiosAndCurvePoints()
    {
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { ActionType = 0, Quantity = 300 },
            new TradeExecutionResultApiDto { ActionType = 1, Quantity = 100 },
            new TradeExecutionResultApiDto { ActionType = 0, Quantity = 200 }
        };

        var snapshot = TradeSupplyDemandCurveBuilder.Build(transactions, maxPoints: 10);

        Assert.Equal(500, snapshot.DemandUnits);
        Assert.Equal(100, snapshot.SupplyUnits);
        Assert.Equal(0.833m, snapshot.DemandRatio);
        Assert.Equal(0.167m, snapshot.SupplyRatio);
        Assert.Equal(3, snapshot.Points.Count);
        Assert.All(snapshot.Points, static point => Assert.True(point.Height >= 2d));
    }

    [Fact]
    public void Build_HandlesEmptyTransactions()
    {
        var snapshot = TradeSupplyDemandCurveBuilder.Build([], maxPoints: 8);

        Assert.Equal(0, snapshot.DemandUnits);
        Assert.Equal(0, snapshot.SupplyUnits);
        Assert.Empty(snapshot.Points);
    }
}
