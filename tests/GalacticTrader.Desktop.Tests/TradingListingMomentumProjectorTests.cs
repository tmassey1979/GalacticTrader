using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradingListingMomentumProjectorTests
{
    [Fact]
    public void Build_ProjectsListingMomentumDirection()
    {
        var listingA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var listingB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { MarketListingId = listingA, UnitPrice = 15m, Quantity = 3 },
            new TradeExecutionResultApiDto { MarketListingId = listingA, UnitPrice = 12m, Quantity = 2 },
            new TradeExecutionResultApiDto { MarketListingId = listingB, UnitPrice = 8m, Quantity = 1 },
            new TradeExecutionResultApiDto { MarketListingId = listingB, UnitPrice = 10m, Quantity = 1 }
        };

        var rows = TradingListingMomentumProjector.Build(transactions, maxRows: 5);

        Assert.Equal(2, rows.Count);
        Assert.Equal("aaaaaaaa", rows[0].ListingId);
        Assert.Equal("Up", rows[0].Movement);
        Assert.Equal("bbbbbbbb", rows[1].ListingId);
        Assert.Equal("Down", rows[1].Movement);
    }
}
