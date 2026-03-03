using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Trading;

public static class TradingListingSummaryProjector
{
    public static IReadOnlyList<TradingListingSummaryDisplayRow> Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        int maxRows = 8)
    {
        return transactions
            .GroupBy(static tx => tx.MarketListingId)
            .Select(group => new TradingListingSummaryDisplayRow
            {
                ListingId = group.Key.ToString()[..8],
                TotalQuantity = group.Sum(static tx => tx.Quantity),
                TotalValue = decimal.Round(group.Sum(static tx => tx.TotalPrice), 2, MidpointRounding.AwayFromZero),
                AverageUnitPrice = decimal.Round(group.Average(static tx => tx.UnitPrice), 2, MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(static row => row.TotalValue)
            .ThenByDescending(static row => row.TotalQuantity)
            .Take(Math.Max(1, maxRows))
            .ToArray();
    }
}
