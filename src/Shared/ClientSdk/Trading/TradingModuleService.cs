using System.Net;
using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Trading;

public sealed class TradingModuleService
{
    private const decimal DefaultFeeRate = 0.05m;
    private readonly TradingDataSource _dataSource;

    public TradingModuleService(TradingDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<TradingModuleState> LoadStateAsync(
        Guid playerId,
        int listingLimit = 80,
        int transactionLimit = 40,
        CancellationToken cancellationToken = default)
    {
        var listingsTask = _dataSource.LoadListingsAsync(Math.Clamp(listingLimit, 1, 300), cancellationToken);
        var transactionsTask = _dataSource.LoadTransactionsAsync(playerId, Math.Clamp(transactionLimit, 1, 200), cancellationToken);
        await Task.WhenAll(listingsTask, transactionsTask);

        var listings = await listingsTask;
        var transactions = await transactionsTask;
        var summaries = BuildListingSummaries(listings, transactions);
        var credits = transactions.Select(static row => row.RemainingPlayerCredits).FirstOrDefault();
        return new TradingModuleState(
            listings,
            transactions,
            summaries,
            credits,
            DateTime.UtcNow);
    }

    public async Task<TradingPreviewResult> PreviewTradeAsync(
        Guid playerId,
        PricePreviewApiRequest request,
        long quantity,
        int transactionSampleSize = 40,
        CancellationToken cancellationToken = default)
    {
        var previewTask = _dataSource.PreviewPriceAsync(request, cancellationToken);
        var transactionsTask = _dataSource.LoadTransactionsAsync(playerId, Math.Clamp(transactionSampleSize, 5, 200), cancellationToken);
        await Task.WhenAll(previewTask, transactionsTask);

        var preview = await previewTask;
        var summary = BuildPreviewSummary(
            preview,
            await transactionsTask,
            quantity);
        return new TradingPreviewResult(preview, summary);
    }

    public async Task<TradingOperationResult> ExecuteTradeAsync(
        ExecuteTradeApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return TradingOperationResult.Failure(
                TradingOperationFailureState.Validation,
                "Quantity must be greater than zero.");
        }

        try
        {
            var result = await _dataSource.ExecuteTradeAsync(request, cancellationToken);
            var actionLabel = request.ActionType == (int)TradingTradeAction.Sell ? "Sell" : "Buy";
            var message = $"{actionLabel} executed: {result.Quantity:N0} units @ {result.UnitPrice:N2}.";
            return TradingOperationResult.Success(result, message);
        }
        catch (Exception exception)
        {
            var failureState = ResolveFailureState(exception);
            return TradingOperationResult.Failure(
                failureState,
                ResolveFailureMessage(failureState),
                ExtractFailureDetail(exception));
        }
    }

    internal static IReadOnlyList<TradingListingSummary> BuildListingSummaries(
        IReadOnlyList<MarketListingApiDto> listings,
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        int maxRows = 25)
    {
        var defaultFeeRate = ResolveFeeRate(transactions);
        var transactionsByListing = transactions
            .GroupBy(static transaction => transaction.MarketListingId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        var summaries = listings
            .Select(listing =>
            {
                var listingTransactions = transactionsByListing.TryGetValue(listing.MarketListingId, out var rows)
                    ? rows
                    : [];
                var averageRecentPrice = listingTransactions.Length == 0
                    ? listing.CurrentPrice
                    : listingTransactions.Average(static row => row.UnitPrice);

                var spread = listing.CurrentPrice - averageRecentPrice;
                var spreadPercent = averageRecentPrice == 0m
                    ? 0m
                    : spread / averageRecentPrice * 100m;

                var listingFeeRate = listingTransactions.Length == 0
                    ? defaultFeeRate
                    : ResolveFeeRate(listingTransactions);

                return new TradingListingSummary(
                    listing.MarketListingId,
                    listing.CommodityName,
                    listing.SectorName,
                    listing.CurrentPrice,
                    listing.AvailableQuantity,
                    averageRecentPrice,
                    spread,
                    spreadPercent,
                    listingFeeRate,
                    decimal.Round(listing.CurrentPrice * listingFeeRate, 4));
            })
            .OrderByDescending(static row => Math.Abs(row.SpreadPercent))
            .ThenByDescending(static row => row.AvailableQuantity)
            .Take(Math.Clamp(maxRows, 1, 200))
            .ToArray();

        return summaries;
    }

    internal static TradingPreviewSummary BuildPreviewSummary(
        PricePreviewApiResultDto preview,
        IReadOnlyList<TradeExecutionResultApiDto> recentTransactions,
        long quantity)
    {
        var normalizedQuantity = Math.Max(1, quantity);
        var spread = preview.CalculatedPrice - preview.CurrentPrice;
        var spreadPercent = preview.CurrentPrice == 0m
            ? 0m
            : spread / preview.CurrentPrice * 100m;
        var feeRate = ResolveFeeRate(recentTransactions);
        return new TradingPreviewSummary
        {
            MarketListingId = preview.MarketListingId,
            CurrentPrice = preview.CurrentPrice,
            CalculatedPrice = preview.CalculatedPrice,
            Spread = spread,
            SpreadPercent = spreadPercent,
            EstimatedFeeRate = feeRate,
            EstimatedFeeAmount = decimal.Round(preview.CalculatedPrice * normalizedQuantity * feeRate, 4),
            Quantity = normalizedQuantity
        };
    }

    private static decimal ResolveFeeRate(IEnumerable<TradeExecutionResultApiDto> transactions)
    {
        var rates = transactions
            .Where(static transaction => transaction.Subtotal > 0m && transaction.TariffAmount >= 0m)
            .Select(static transaction => transaction.TariffAmount / transaction.Subtotal)
            .ToArray();

        if (rates.Length == 0)
        {
            return DefaultFeeRate;
        }

        var boundedAverage = rates.Average();
        return decimal.Round(Math.Clamp(boundedAverage, 0m, 1m), 4);
    }

    private static TradingOperationFailureState ResolveFailureState(Exception exception)
    {
        if (exception is ApiClientException apiException)
        {
            if (apiException.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return TradingOperationFailureState.Unauthorized;
            }
        }

        var detail = ExtractFailureDetail(exception);
        if (detail.Contains("Insufficient player credits", StringComparison.OrdinalIgnoreCase))
        {
            return TradingOperationFailureState.InsufficientCredits;
        }

        if (detail.Contains("Insufficient cargo capacity", StringComparison.OrdinalIgnoreCase))
        {
            return TradingOperationFailureState.InsufficientCargoCapacity;
        }

        if (detail.Contains("Insufficient quantity", StringComparison.OrdinalIgnoreCase))
        {
            return TradingOperationFailureState.InsufficientMarketQuantity;
        }

        if (detail.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return TradingOperationFailureState.RateLimited;
        }

        if (detail.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("must be", StringComparison.OrdinalIgnoreCase))
        {
            return TradingOperationFailureState.Validation;
        }

        return TradingOperationFailureState.Unknown;
    }

    private static string ResolveFailureMessage(TradingOperationFailureState failureState)
    {
        return failureState switch
        {
            TradingOperationFailureState.InsufficientCredits => "Not enough credits to execute this trade.",
            TradingOperationFailureState.InsufficientCargoCapacity => "Cargo hold is full for this quantity.",
            TradingOperationFailureState.InsufficientMarketQuantity => "Market listing does not have enough quantity available.",
            TradingOperationFailureState.RateLimited => "Trading is temporarily throttled. Wait briefly and retry.",
            TradingOperationFailureState.Unauthorized => "Session is invalid or expired. Sign in again.",
            TradingOperationFailureState.Validation => "Trade request is invalid. Verify ship, listing, and quantity.",
            _ => "Trade could not be completed. Refresh market data and retry."
        };
    }

    private static string ExtractFailureDetail(Exception exception)
    {
        return exception is ApiClientException apiException
            ? apiException.Detail
            : exception.Message;
    }
}
