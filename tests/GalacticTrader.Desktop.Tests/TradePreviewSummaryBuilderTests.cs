using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradePreviewSummaryBuilderTests
{
    [Fact]
    public void Build_UsesHistoricalTariffRate_WhenTransactionsExist()
    {
        var preview = new PricePreviewApiResultDto
        {
            MarketListingId = Guid.NewGuid(),
            CurrentPrice = 120m,
            CalculatedPrice = 132m
        };

        var transactions = new[]
        {
            new TradeExecutionResultApiDto { Subtotal = 100m, TariffAmount = 5m },
            new TradeExecutionResultApiDto { Subtotal = 200m, TariffAmount = 20m }
        };

        var summary = TradePreviewSummaryBuilder.Build(preview, transactions, quantity: 10);

        Assert.Equal(12m, summary.Spread);
        Assert.Equal(10m, summary.SpreadPercent);
        Assert.Equal(0.075m, summary.EstimatedTariffRate);
        Assert.Equal(99m, summary.EstimatedTariffAmount);
    }

    [Fact]
    public void Build_FallsBackToDefaultTariffRate_WhenNoValidTransactions()
    {
        var preview = new PricePreviewApiResultDto
        {
            MarketListingId = Guid.NewGuid(),
            CurrentPrice = 50m,
            CalculatedPrice = 75m
        };

        var summary = TradePreviewSummaryBuilder.Build(preview, [], quantity: 4);

        Assert.Equal(0.05m, summary.EstimatedTariffRate);
        Assert.Equal(15m, summary.EstimatedTariffAmount);
    }

    [Fact]
    public void Build_ProtectsAgainstZeroCurrentPrice()
    {
        var preview = new PricePreviewApiResultDto
        {
            MarketListingId = Guid.NewGuid(),
            CurrentPrice = 0m,
            CalculatedPrice = 19m
        };

        var summary = TradePreviewSummaryBuilder.Build(preview, [], quantity: 1);

        Assert.Equal(0m, summary.SpreadPercent);
    }
}
