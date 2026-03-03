namespace GalacticTrader.Desktop.Modules;

public sealed class MarketTraderDisplayRow
{
    public required string Username { get; init; }
    public required decimal TradeVolume { get; init; }
    public required int TradeCount { get; init; }
}
