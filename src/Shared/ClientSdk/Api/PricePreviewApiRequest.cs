namespace GalacticTrader.Desktop.Api;

public sealed class PricePreviewApiRequest
{
    public Guid MarketListingId { get; init; }
    public float DemandMultiplier { get; init; }
    public float RiskPremium { get; init; }
    public float ScarcityModifier { get; init; }
    public float FactionStabilityModifier { get; init; }
    public float PirateActivityModifier { get; init; }
    public float MonopolyModifier { get; init; }
}
