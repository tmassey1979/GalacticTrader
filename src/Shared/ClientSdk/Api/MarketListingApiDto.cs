namespace GalacticTrader.Desktop.Api;

public sealed class MarketListingApiDto
{
    public Guid MarketListingId { get; init; }
    public Guid MarketId { get; init; }
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public Guid CommodityId { get; init; }
    public string CommodityName { get; init; } = string.Empty;
    public decimal CurrentPrice { get; init; }
    public decimal BasePrice { get; init; }
    public long AvailableQuantity { get; init; }
    public float DemandMultiplier { get; init; }
    public float RiskPremium { get; init; }
    public float ScarcityModifier { get; init; }
    public float PriceChangePercent24h { get; init; }
    public float LegalityFactor { get; init; }
}
