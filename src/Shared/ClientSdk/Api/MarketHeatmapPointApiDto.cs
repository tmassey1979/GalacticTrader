namespace GalacticTrader.Desktop.Api;

public sealed class MarketHeatmapPointApiDto
{
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public decimal TradeVolume { get; init; }
    public int TradeCount { get; init; }
}
