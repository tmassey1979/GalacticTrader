using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Trading;

public static class TradeHeatmapProjector
{
    public static IReadOnlyList<TradeHeatmapDisplayRow> Build(
        MarketIntelligenceSummaryApiDto summary,
        int maxRows = 6)
    {
        return summary.RegionalHeatmap
            .OrderByDescending(static point => point.TradeVolume)
            .ThenBy(static point => point.SectorName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, maxRows))
            .Select(static point => new TradeHeatmapDisplayRow
            {
                SectorName = point.SectorName,
                TradeVolume = point.TradeVolume,
                TradeCount = point.TradeCount
            })
            .ToArray();
    }
}
