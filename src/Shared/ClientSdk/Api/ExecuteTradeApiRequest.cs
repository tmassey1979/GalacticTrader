namespace GalacticTrader.Desktop.Api;

public sealed class ExecuteTradeApiRequest
{
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public Guid MarketListingId { get; init; }
    public int ActionType { get; init; }
    public long Quantity { get; init; }
    public decimal? ExpectedUnitPrice { get; init; }
}
