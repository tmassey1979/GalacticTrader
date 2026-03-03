namespace GalacticTrader.Desktop.Trading;

public sealed class TradeHeatmapDisplayRow
{
    public string SectorName { get; init; } = string.Empty;
    public decimal TradeVolume { get; init; }
    public int TradeCount { get; init; }
}
