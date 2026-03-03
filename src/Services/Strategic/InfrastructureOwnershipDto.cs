namespace GalacticTrader.Services.Strategic;

public sealed class InfrastructureOwnershipDto
{
    public Guid Id { get; init; }
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public string InfrastructureType { get; init; } = string.Empty;
    public float ControlScore { get; init; }
    public DateTime ClaimedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
