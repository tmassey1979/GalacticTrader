using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class MarketShockEventProjector
{
    public static IReadOnlyList<EventFeedEntry> Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        DateTime occurredAtUtc,
        int maxEvents = 3)
    {
        return transactions
            .GroupBy(static tx => tx.MarketListingId)
            .Select(group =>
            {
                var prices = group.Select(static tx => tx.UnitPrice).ToArray();
                if (prices.Length < 3)
                {
                    return (HasShock: false, Entry: (EventFeedEntry?)null, Volatility: 0m);
                }

                var avg = prices.Average();
                if (avg <= 0m)
                {
                    return (HasShock: false, Entry: (EventFeedEntry?)null, Volatility: 0m);
                }

                var min = prices.Min();
                var max = prices.Max();
                var swing = max - min;
                var volatility = swing / avg;
                var isShock = volatility >= 0.18m;
                var entry = new EventFeedEntry
                {
                    OccurredAtUtc = occurredAtUtc,
                    Category = "Market",
                    Title = $"Market Shock {group.Key.ToString()[..8]}",
                    Detail = $"Volatility {volatility:P1} | Avg {avg:N2} | Swing {swing:N2} | Trades {prices.Length}"
                };
                return (HasShock: isShock, Entry: entry, Volatility: volatility);
            })
            .Where(static candidate => candidate.HasShock)
            .OrderByDescending(static candidate => candidate.Volatility)
            .Take(Math.Max(1, maxEvents))
            .Select(static candidate => candidate.Entry!)
            .ToArray();
    }
}
