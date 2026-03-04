using System.Net;
using GalacticTrader.ClientSdk.Trading;
using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradingModuleServiceTests
{
    [Fact]
    public async Task LoadStateAsync_CombinesListingsTransactionsAndDerivedSummaries()
    {
        var playerId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var dataSource = new TradingDataSource
        {
            LoadListingsAsync = (_, _) => Task.FromResult<IReadOnlyList<MarketListingApiDto>>(
            [
                new MarketListingApiDto
                {
                    MarketListingId = listingId,
                    CommodityName = "Fuel Ore",
                    SectorName = "Alpha",
                    CurrentPrice = 120m,
                    AvailableQuantity = 5000
                }
            ]),
            LoadTransactionsAsync = (_, _, _) => Task.FromResult<IReadOnlyList<TradeExecutionResultApiDto>>(
            [
                new TradeExecutionResultApiDto
                {
                    MarketListingId = listingId,
                    UnitPrice = 100m,
                    Subtotal = 1000m,
                    TariffAmount = 50m,
                    RemainingPlayerCredits = 20000m
                },
                new TradeExecutionResultApiDto
                {
                    MarketListingId = listingId,
                    UnitPrice = 110m,
                    Subtotal = 1100m,
                    TariffAmount = 55m
                }
            ]),
            PreviewPriceAsync = (_, _) => Task.FromResult(new PricePreviewApiResultDto()),
            ExecuteTradeAsync = (_, _) => Task.FromResult(new TradeExecutionResultApiDto())
        };
        var service = new TradingModuleService(dataSource);

        var state = await service.LoadStateAsync(playerId);

        Assert.Single(state.Listings);
        Assert.Equal(2, state.Transactions.Count);
        Assert.Equal(20000m, state.AvailableCredits);
        var summary = Assert.Single(state.ListingSummaries);
        Assert.Equal(listingId, summary.MarketListingId);
        Assert.Equal(15m, decimal.Round(summary.Spread, 2));
        Assert.Equal(14.2857m, decimal.Round(summary.SpreadPercent, 4));
        Assert.Equal(0.05m, summary.EstimatedFeeRate);
    }

    [Fact]
    public async Task PreviewTradeAsync_BuildsSpreadAndEstimatedFeeFromRecentHistory()
    {
        var playerId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var dataSource = new TradingDataSource
        {
            LoadListingsAsync = (_, _) => Task.FromResult<IReadOnlyList<MarketListingApiDto>>([]),
            LoadTransactionsAsync = (_, _, _) => Task.FromResult<IReadOnlyList<TradeExecutionResultApiDto>>(
            [
                new TradeExecutionResultApiDto
                {
                    MarketListingId = listingId,
                    Subtotal = 1000m,
                    TariffAmount = 100m
                }
            ]),
            PreviewPriceAsync = (_, _) => Task.FromResult(new PricePreviewApiResultDto
            {
                MarketListingId = listingId,
                CurrentPrice = 100m,
                CalculatedPrice = 110m
            }),
            ExecuteTradeAsync = (_, _) => Task.FromResult(new TradeExecutionResultApiDto())
        };
        var service = new TradingModuleService(dataSource);

        var result = await service.PreviewTradeAsync(
            playerId,
            new PricePreviewApiRequest { MarketListingId = listingId },
            quantity: 10);

        Assert.Equal(10m, result.Summary.Spread);
        Assert.Equal(10m, result.Summary.SpreadPercent);
        Assert.Equal(0.1m, result.Summary.EstimatedFeeRate);
        Assert.Equal(110m, result.Summary.EstimatedFeeAmount);
    }

    [Fact]
    public async Task ExecuteTradeAsync_MapsInsufficientCreditsErrorToFriendlyFailure()
    {
        var dataSource = new TradingDataSource
        {
            LoadListingsAsync = (_, _) => Task.FromResult<IReadOnlyList<MarketListingApiDto>>([]),
            LoadTransactionsAsync = (_, _, _) => Task.FromResult<IReadOnlyList<TradeExecutionResultApiDto>>([]),
            PreviewPriceAsync = (_, _) => Task.FromResult(new PricePreviewApiResultDto()),
            ExecuteTradeAsync = (_, _) => throw new ApiClientException(
                "Execute trade",
                HttpStatusCode.BadRequest,
                "Insufficient player credits.")
        };
        var service = new TradingModuleService(dataSource);

        var result = await service.ExecuteTradeAsync(new ExecuteTradeApiRequest
        {
            PlayerId = Guid.NewGuid(),
            ShipId = Guid.NewGuid(),
            MarketListingId = Guid.NewGuid(),
            ActionType = (int)TradingTradeAction.Buy,
            Quantity = 50
        });

        Assert.False(result.Succeeded);
        Assert.Equal(TradingOperationFailureState.InsufficientCredits, result.FailureState);
        Assert.Contains("Not enough credits", result.Message);
    }

    [Fact]
    public async Task ExecuteTradeAsync_ReturnsSuccessWhenTransactionCompletes()
    {
        var dataSource = new TradingDataSource
        {
            LoadListingsAsync = (_, _) => Task.FromResult<IReadOnlyList<MarketListingApiDto>>([]),
            LoadTransactionsAsync = (_, _, _) => Task.FromResult<IReadOnlyList<TradeExecutionResultApiDto>>([]),
            PreviewPriceAsync = (_, _) => Task.FromResult(new PricePreviewApiResultDto()),
            ExecuteTradeAsync = (_, _) => Task.FromResult(new TradeExecutionResultApiDto
            {
                Quantity = 25,
                UnitPrice = 89.5m
            })
        };
        var service = new TradingModuleService(dataSource);

        var result = await service.ExecuteTradeAsync(new ExecuteTradeApiRequest
        {
            PlayerId = Guid.NewGuid(),
            ShipId = Guid.NewGuid(),
            MarketListingId = Guid.NewGuid(),
            ActionType = (int)TradingTradeAction.Buy,
            Quantity = 25
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Transaction);
        Assert.Contains("executed", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
