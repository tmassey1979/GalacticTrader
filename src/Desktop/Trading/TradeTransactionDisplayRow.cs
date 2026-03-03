namespace GalacticTrader.Desktop.Trading;

public sealed class TradeTransactionDisplayRow
{
    public required string ListingId { get; init; }
    public required string Action { get; init; }
    public long Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TariffAmount { get; init; }
    public decimal TotalPrice { get; init; }
    public required string Status { get; init; }
}
