namespace GalacticTrader.Services.Market;

public sealed class ExecuteTradeRequest
{
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public Guid MarketListingId { get; init; }
    public TradeActionType ActionType { get; init; }
    public long Quantity { get; init; }
    public decimal? ExpectedUnitPrice { get; init; }
}
