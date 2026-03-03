namespace GalacticTrader.Desktop.Api;

public sealed class PricePreviewApiResultDto
{
    public Guid MarketListingId { get; init; }
    public decimal BasePrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal CalculatedPrice { get; init; }
    public float DemandMultiplier { get; init; }
    public float RiskPremium { get; init; }
    public float ScarcityModifier { get; init; }
    public float MarketEfficiency { get; init; }
}
