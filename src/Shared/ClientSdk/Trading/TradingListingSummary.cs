namespace GalacticTrader.ClientSdk.Trading;

public sealed record TradingListingSummary(
    Guid MarketListingId,
    string CommodityName,
    string SectorName,
    decimal CurrentPrice,
    long AvailableQuantity,
    decimal AverageRecentPrice,
    decimal Spread,
    decimal SpreadPercent,
    decimal EstimatedFeeRate,
    decimal EstimatedFeePerUnit);
