namespace GalacticTrader.Services.Strategic;

public sealed class UpdateInfrastructureOwnershipRequest
{
    public Guid SectorId { get; init; }
    public Guid FactionId { get; init; }
    public string InfrastructureType { get; init; } = string.Empty;
    public float ControlScore { get; init; }
}
