namespace GalacticTrader.Services.Economy;

public sealed class CommodityHierarchyItem
{
    public Guid CommodityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string HierarchyTier { get; init; } = string.Empty;
    public float LegalityFactor { get; init; }
    public float Rarity { get; init; }
}
