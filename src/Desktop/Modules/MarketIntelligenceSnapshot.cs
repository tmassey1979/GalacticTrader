namespace GalacticTrader.Desktop.Modules;

public sealed class MarketIntelligenceSnapshot
{
    public decimal VolatilityIndex { get; init; }
    public required string VolatilityTrendSummary { get; init; }
    public IReadOnlyList<MarketHeatmapDisplayRow> Heatmap { get; init; } = [];
    public IReadOnlyList<MarketTraderDisplayRow> TopTraders { get; init; } = [];
    public IReadOnlyList<SmugglingCorridorDisplayRow> SmugglingCorridors { get; init; } = [];
    public IReadOnlyList<MarketTradeFlowDisplayRow> TradeFlows { get; init; } = [];
}
