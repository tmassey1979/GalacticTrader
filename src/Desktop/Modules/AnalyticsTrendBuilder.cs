using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class AnalyticsTrendBuilder
{
    public static IReadOnlyList<AnalyticsTrendBar> BuildRevenueBars(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        int maxPoints = 12,
        double maxHeight = 74d)
    {
        var values = transactions
            .Take(Math.Max(1, maxPoints))
            .Select(static transaction => Math.Max(0m, transaction.TotalPrice))
            .Reverse()
            .ToArray();

        if (values.Length == 0)
        {
            return [];
        }

        var peak = values.Max();
        if (peak <= 0m)
        {
            peak = 1m;
        }

        var bars = new List<AnalyticsTrendBar>(values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            var normalized = (double)(value / peak);
            bars.Add(new AnalyticsTrendBar
            {
                Index = index,
                Value = value,
                Height = Math.Max(4d, normalized * maxHeight)
            });
        }

        return bars;
    }
}
