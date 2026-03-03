using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradingTransactionFilterTests
{
    [Fact]
    public void Apply_FiltersByActionAndListingKeyword()
    {
        var rows = new[]
        {
            new TradeTransactionDisplayRow { ListingId = "AAAA1111", Action = "Buy", Quantity = 10, UnitPrice = 4m, TariffAmount = 1m, TotalPrice = 41m, Status = "ok" },
            new TradeTransactionDisplayRow { ListingId = "BBBB2222", Action = "Sell", Quantity = 4, UnitPrice = 9m, TariffAmount = 0.5m, TotalPrice = 35.5m, Status = "ok" },
            new TradeTransactionDisplayRow { ListingId = "AAAA9999", Action = "Buy", Quantity = 2, UnitPrice = 10m, TariffAmount = 0.4m, TotalPrice = 20.4m, Status = "ok" }
        };

        var filtered = TradingTransactionFilter.Apply(rows, new TradingTransactionFilterOptions
        {
            Action = "Buy",
            ListingKeyword = "AAAA"
        });

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, row => Assert.Equal("Buy", row.Action));
    }
}
