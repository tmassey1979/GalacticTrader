namespace GalacticTrader.Services.Telemetry;

public sealed class MarketHeatmapPointDto
{
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public decimal TradeVolume { get; init; }
    public int TradeCount { get; init; }
}
