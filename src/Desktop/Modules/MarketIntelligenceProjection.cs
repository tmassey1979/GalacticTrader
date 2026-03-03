using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class MarketIntelligenceProjection
{
    public static MarketIntelligenceSnapshot Build(MarketIntelligenceSummaryApiDto summary)
    {
        var volatility = decimal.Round(summary.VolatilityIndex, 1);
        var volatilityTrend = MarketVolatilityTrendProjector.Build(volatility);

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

        var flowRows = summary.SmugglingCorridors
            .Select(corridor =>
            {
                var flowIndex = Math.Round(Math.Clamp((corridor.SmugglingRuns * 8m) + (corridor.AverageTradeValue / 12m), 0m, 100m), 1);
                return new MarketTradeFlowDisplayRow
                {
                    Flow = $"{corridor.FromSectorName} -> {corridor.ToSectorName}",
                    FlowIndex = flowIndex,
                    Diagram = MarketTradeFlowDiagramBuilder.Build(flowIndex)
                };
            })
            .OrderByDescending(static row => row.FlowIndex)
            .ThenBy(static row => row.Flow, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        return new MarketIntelligenceSnapshot
        {
            VolatilityIndex = volatility,
            VolatilityTrendSummary = volatilityTrend.Summary,
            Heatmap = heatmap,
            TopTraders = traders,
            SmugglingCorridors = corridors,
            TradeFlows = flowRows
        };
    }
}
