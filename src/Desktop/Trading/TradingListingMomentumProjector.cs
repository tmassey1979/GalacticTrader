using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Trading;

public static class TradingListingMomentumProjector
{
    public static IReadOnlyList<TradingListingMomentumDisplayRow> Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        int maxRows = 8)
    {
        return transactions
            .GroupBy(static tx => tx.MarketListingId)
            .Select(group =>
            {
                var ordered = group.ToArray();
                var latestPrice = ordered[0].UnitPrice;
                var earliestPrice = ordered[^1].UnitPrice;
                var delta = latestPrice - earliestPrice;
                return new TradingListingMomentumDisplayRow
                {
                    ListingId = group.Key.ToString()[..8],
                    TradeCount = ordered.Length,
                    AveragePrice = Math.Round(ordered.Average(static tx => tx.UnitPrice), 2, MidpointRounding.AwayFromZero),
                    Delta = Math.Round(delta, 2, MidpointRounding.AwayFromZero),
                    Movement = ResolveMovement(delta)
                };
            })
            .OrderByDescending(static row => Math.Abs(row.Delta))
            .ThenByDescending(static row => row.TradeCount)
            .Take(Math.Max(1, maxRows))
            .ToArray();
    }

    private static string ResolveMovement(decimal delta)
    {
        if (delta > 0.01m)
        {
            return "Up";
        }

        if (delta < -0.01m)
        {
            return "Down";
        }

        return "Flat";
    }
}
