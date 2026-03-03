namespace GalacticTrader.Desktop.Api;

public sealed class MarketIntelligenceSummaryApiDto
{
    public decimal VolatilityIndex { get; init; }
    public IReadOnlyList<MarketHeatmapPointApiDto> RegionalHeatmap { get; init; } = [];
    public IReadOnlyList<TopTraderInsightApiDto> TopTraders { get; init; } = [];
    public IReadOnlyList<SmugglingCorridorInsightApiDto> SmugglingCorridors { get; init; } = [];
}
