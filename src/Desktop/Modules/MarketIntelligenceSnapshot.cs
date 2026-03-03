namespace GalacticTrader.Desktop.Modules;

public sealed class MarketIntelligenceSnapshot
{
    public decimal VolatilityIndex { get; init; }
    public IReadOnlyList<MarketHeatmapDisplayRow> Heatmap { get; init; } = [];
    public IReadOnlyList<MarketTraderDisplayRow> TopTraders { get; init; } = [];
    public IReadOnlyList<SmugglingCorridorDisplayRow> SmugglingCorridors { get; init; } = [];
}
