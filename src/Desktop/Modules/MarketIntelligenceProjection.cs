using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class MarketIntelligenceProjection
{
    public static MarketIntelligenceSnapshot Build(MarketIntelligenceSummaryApiDto summary)
    {
        var heatmap = summary.RegionalHeatmap
            .OrderByDescending(static point => point.TradeVolume)
            .Select(point => new MarketHeatmapDisplayRow
            {
                SectorName = point.SectorName,
                TradeVolume = Math.Round(point.TradeVolume, 2),
                TradeCount = point.TradeCount
            })
            .ToArray();

        var traders = summary.TopTraders
            .OrderByDescending(static trader => trader.TradeVolume)
            .Select(trader => new MarketTraderDisplayRow
            {
                Username = trader.Username,
                TradeVolume = Math.Round(trader.TradeVolume, 2),
                TradeCount = trader.TradeCount
            })
            .ToArray();

        var corridors = summary.SmugglingCorridors
            .OrderByDescending(static corridor => corridor.SmugglingRuns)
            .ThenByDescending(static corridor => corridor.AverageTradeValue)
            .Select(corridor => new SmugglingCorridorDisplayRow
            {
                Corridor = $"{corridor.FromSectorName} -> {corridor.ToSectorName}",
                SmugglingRuns = corridor.SmugglingRuns,
                AverageTradeValue = Math.Round(corridor.AverageTradeValue, 2)
            })
            .ToArray();

        return new MarketIntelligenceSnapshot
        {
            VolatilityIndex = Math.Round(summary.VolatilityIndex, 1),
            Heatmap = heatmap,
            TopTraders = traders,
            SmugglingCorridors = corridors
        };
    }
}
