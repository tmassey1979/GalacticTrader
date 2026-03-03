namespace GalacticTrader.Services.Telemetry;

public sealed class MarketIntelligenceSummaryDto
{
    public decimal VolatilityIndex { get; init; }
    public IReadOnlyList<MarketHeatmapPointDto> RegionalHeatmap { get; init; } = [];
    public IReadOnlyList<TopTraderInsightDto> TopTraders { get; init; } = [];
    public IReadOnlyList<SmugglingCorridorInsightDto> SmugglingCorridors { get; init; } = [];
}
