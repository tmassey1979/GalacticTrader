namespace GalacticTrader.Desktop.Modules;

public sealed class MarketHeatmapDisplayRow
{
    public required string SectorName { get; init; }
    public required decimal TradeVolume { get; init; }
    public required int TradeCount { get; init; }
}
