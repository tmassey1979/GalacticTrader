namespace GalacticTrader.Services.Economy;

public sealed class PriceCalculationInput
{
    public Guid MarketListingId { get; init; }
    public float DemandMultiplier { get; init; }
    public float RiskPremium { get; init; }
    public float ScarcityModifier { get; init; }
    public float FactionStabilityModifier { get; init; }
    public float PirateActivityModifier { get; init; }
    public float MonopolyModifier { get; init; }
}

public sealed class PriceCalculationResult
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

public sealed class MarketTickResult
{
    public DateTime ProcessedAtUtc { get; init; }
    public int MarketsProcessed { get; init; }
    public int ListingsUpdated { get; init; }
    public int ShockEventsTriggered { get; init; }
}

public sealed class MarketShockRequest
{
    public Guid MarketId { get; init; }
    public float Intensity { get; init; } = 0.25f;
    public string Reason { get; init; } = "Unspecified";
}

public sealed class CommodityHierarchyItem
{
    public Guid CommodityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string HierarchyTier { get; init; } = string.Empty;
    public float LegalityFactor { get; init; }
    public float Rarity { get; init; }
}
