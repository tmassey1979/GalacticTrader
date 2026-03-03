using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradingListingSummaryProjectorTests
{
    [Fact]
    public void Build_AggregatesListingQuantitiesValuesAndAveragePrice()
    {
        var listingA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var listingB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { MarketListingId = listingA, Quantity = 10, TotalPrice = 90m, UnitPrice = 9m },
            new TradeExecutionResultApiDto { MarketListingId = listingA, Quantity = 5, TotalPrice = 55m, UnitPrice = 11m },
            new TradeExecutionResultApiDto { MarketListingId = listingB, Quantity = 3, TotalPrice = 120m, UnitPrice = 40m }
        };

        var rows = TradingListingSummaryProjector.Build(transactions, maxRows: 5);

        Assert.Equal(2, rows.Count);
        Assert.Equal("aaaaaaaa", rows[0].ListingId);
        Assert.Equal(15, rows[0].TotalQuantity);
        Assert.Equal(145m, rows[0].TotalValue);
        Assert.Equal(10m, rows[0].AverageUnitPrice);

        Assert.Equal("bbbbbbbb", rows[1].ListingId);
        Assert.Equal(3, rows[1].TotalQuantity);
        Assert.Equal(120m, rows[1].TotalValue);
        Assert.Equal(40m, rows[1].AverageUnitPrice);
    }
}
