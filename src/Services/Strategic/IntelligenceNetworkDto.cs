namespace GalacticTrader.Services.Strategic;

public sealed class IntelligenceNetworkDto
{
    public Guid Id { get; init; }
    public Guid OwnerPlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AssetCount { get; init; }
    public float CoverageScore { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
}
