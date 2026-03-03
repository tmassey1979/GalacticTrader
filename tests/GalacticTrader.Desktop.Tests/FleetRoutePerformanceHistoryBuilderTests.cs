using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Fleet;

namespace GalacticTrader.Desktop.Tests;

public sealed class FleetRoutePerformanceHistoryBuilderTests
{
    [Fact]
    public void Build_FiltersByShipAndBuildsEntries()
    {
        var shipA = Guid.NewGuid();
        var shipB = Guid.NewGuid();
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { ShipId = shipA, ActionType = 0, Quantity = 100, TotalPrice = 1200m, TariffAmount = 30m, Status = "filled" },
            new TradeExecutionResultApiDto { ShipId = shipB, ActionType = 1, Quantity = 25, TotalPrice = 500m, TariffAmount = 8m, Status = "filled" },
            new TradeExecutionResultApiDto { ShipId = shipA, ActionType = 1, Quantity = 55, TotalPrice = 980m, TariffAmount = 14m, Status = "settled" }
        };

        var entries = FleetRoutePerformanceHistoryBuilder.Build(shipA, transactions, maxEntries: 5);

        Assert.Equal(2, entries.Count);
        Assert.Equal("Run 1", entries[0].RunLabel);
        Assert.Equal("Buy", entries[0].Action);
        Assert.Equal(1170m, entries[0].NetValue);
        Assert.Equal("Run 2", entries[1].RunLabel);
        Assert.Equal("Sell", entries[1].Action);
        Assert.Equal(966m, entries[1].NetValue);
    }

    [Fact]
    public void Build_RespectsMaxEntriesAndReturnsEmptyForNoMatch()
    {
        var shipId = Guid.NewGuid();
        var transactions = Enumerable.Range(0, 10)
            .Select(_ => new TradeExecutionResultApiDto
            {
                ShipId = shipId,
                ActionType = 0,
                Quantity = 10,
                TotalPrice = 100m,
                TariffAmount = 5m,
                Status = "filled"
            })
            .ToArray();

        var limited = FleetRoutePerformanceHistoryBuilder.Build(shipId, transactions, maxEntries: 3);
        var empty = FleetRoutePerformanceHistoryBuilder.Build(Guid.NewGuid(), transactions, maxEntries: 3);

        Assert.Equal(3, limited.Count);
        Assert.Empty(empty);
    }
}
