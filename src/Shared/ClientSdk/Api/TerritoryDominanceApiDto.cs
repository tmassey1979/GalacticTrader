namespace GalacticTrader.Desktop.Api;

public sealed class TerritoryDominanceApiDto
{
    public Guid Id { get; init; }
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public int ControlledSectorCount { get; init; }
    public float InfrastructureControlScore { get; init; }
    public float WarMomentumScore { get; init; }
    public float DominanceScore { get; init; }
    public DateTime UpdatedAt { get; init; }
}
