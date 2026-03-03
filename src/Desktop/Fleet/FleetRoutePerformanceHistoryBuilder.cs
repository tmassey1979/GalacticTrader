using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Fleet;

public static class FleetRoutePerformanceHistoryBuilder
{
    public static IReadOnlyList<FleetRoutePerformanceEntry> Build(
        Guid shipId,
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        int maxEntries = 8)
    {
        var normalizedMax = Math.Max(1, maxEntries);
        return transactions
            .Where(transaction => transaction.ShipId == shipId)
            .Take(normalizedMax)
            .Select((transaction, index) => new FleetRoutePerformanceEntry
            {
                RunLabel = $"Run {index + 1}",
                Action = transaction.ActionType == 0 ? "Buy" : "Sell",
                Quantity = transaction.Quantity,
                GrossValue = transaction.TotalPrice,
                Tariff = transaction.TariffAmount,
                NetValue = transaction.TotalPrice - transaction.TariffAmount,
                Status = transaction.Status
            })
            .ToArray();
    }
}
