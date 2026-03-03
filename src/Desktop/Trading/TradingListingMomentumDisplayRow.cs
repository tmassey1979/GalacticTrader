namespace GalacticTrader.Desktop.Trading;

public sealed class TradingListingMomentumDisplayRow
{
    public required string ListingId { get; init; }
    public int TradeCount { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal Delta { get; init; }
    public required string Movement { get; init; }
}
