using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Trading;

public static class TradePreviewSummaryBuilder
{
    private const decimal DefaultTariffRate = 0.05m;

    public static TradePreviewSummary Build(
        PricePreviewApiResultDto preview,
        IReadOnlyList<TradeExecutionResultApiDto> recentTransactions,
        long quantity)
    {
        var normalizedQuantity = Math.Max(1, quantity);
        var spread = preview.CalculatedPrice - preview.CurrentPrice;
        var spreadPercent = preview.CurrentPrice == 0m
            ? 0m
            : spread / preview.CurrentPrice * 100m;

        var observedRates = recentTransactions
            .Where(static transaction => transaction.Subtotal > 0m && transaction.TariffAmount >= 0m)
            .Select(static transaction => transaction.TariffAmount / transaction.Subtotal)
            .ToArray();

        var estimatedTariffRate = observedRates.Length > 0
            ? observedRates.Average()
            : DefaultTariffRate;

        var estimatedTariffAmount = preview.CalculatedPrice * normalizedQuantity * estimatedTariffRate;
        return new TradePreviewSummary
        {
            MarketListingId = preview.MarketListingId,
            CurrentPrice = preview.CurrentPrice,
            CalculatedPrice = preview.CalculatedPrice,
            Spread = spread,
            SpreadPercent = spreadPercent,
            EstimatedTariffRate = estimatedTariffRate,
            EstimatedTariffAmount = estimatedTariffAmount,
            Quantity = normalizedQuantity
        };
    }
}
