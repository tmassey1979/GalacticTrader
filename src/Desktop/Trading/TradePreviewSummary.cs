namespace GalacticTrader.Desktop.Trading;

public sealed class TradePreviewSummary
{
    public required Guid MarketListingId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal CalculatedPrice { get; init; }
    public decimal Spread { get; init; }
    public decimal SpreadPercent { get; init; }
    public decimal EstimatedTariffRate { get; init; }
    public decimal EstimatedTariffAmount { get; init; }
    public long Quantity { get; init; }
}
