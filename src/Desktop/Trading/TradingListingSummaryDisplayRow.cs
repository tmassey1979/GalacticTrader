namespace GalacticTrader.Desktop.Trading;

public sealed class TradingListingSummaryDisplayRow
{
    public required string ListingId { get; init; }
    public long TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
    public decimal AverageUnitPrice { get; init; }
}
