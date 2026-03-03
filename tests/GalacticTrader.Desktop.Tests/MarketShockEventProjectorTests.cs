using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class MarketShockEventProjectorTests
{
    [Fact]
    public void Build_EmitsMarketShockForVolatileListings()
    {
        var listingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { MarketListingId = listingId, UnitPrice = 10m },
            new TradeExecutionResultApiDto { MarketListingId = listingId, UnitPrice = 15m },
            new TradeExecutionResultApiDto { MarketListingId = listingId, UnitPrice = 8m }
        };

        var events = MarketShockEventProjector.Build(transactions, new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc));

        Assert.Single(events);
        Assert.Equal("Market", events[0].Category);
        Assert.Contains("Market Shock", events[0].Title, StringComparison.Ordinal);
    }
}
