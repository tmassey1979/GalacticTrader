namespace GalacticTrader.ClientSdk.Trading;

public sealed class TradingPreviewSummary
{
    public required Guid MarketListingId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal CalculatedPrice { get; init; }
    public decimal Spread { get; init; }
    public decimal SpreadPercent { get; init; }
    public decimal EstimatedFeeRate { get; init; }
    public decimal EstimatedFeeAmount { get; init; }
    public long Quantity { get; init; }
}
