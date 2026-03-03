namespace GalacticTrader.Services.Market;

public sealed class TradeExecutionResult
{
    public Guid TradeTransactionId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public Guid MarketListingId { get; init; }
    public TradeActionType ActionType { get; init; }
    public long Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
    public decimal TariffAmount { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal RemainingPlayerCredits { get; init; }
    public string Status { get; init; } = string.Empty;
}
