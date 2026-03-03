namespace GalacticTrader.Services.Strategic;

public sealed class CreateIntelligenceNetworkRequest
{
    public Guid OwnerPlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AssetCount { get; init; }
    public float CoverageScore { get; init; }
}
