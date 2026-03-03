namespace GalacticTrader.Desktop.Trading;

public static class TradingTransactionFilter
{
    public static IReadOnlyList<TradeTransactionDisplayRow> Apply(
        IReadOnlyList<TradeTransactionDisplayRow> rows,
        TradingTransactionFilterOptions options)
    {
        var keyword = options.ListingKeyword?.Trim() ?? string.Empty;
        var filterAction = options.Action?.Trim() ?? "All";

        return rows
            .Where(row => MatchesAction(row, filterAction))
            .Where(row => string.IsNullOrWhiteSpace(keyword) ||
                          row.ListingId.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static bool MatchesAction(TradeTransactionDisplayRow row, string action)
    {
        return action.Equals("All", StringComparison.OrdinalIgnoreCase) ||
               row.Action.Equals(action, StringComparison.OrdinalIgnoreCase);
    }
}
