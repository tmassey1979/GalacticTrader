namespace GalacticTrader.Services.Strategic;

public sealed class UpdateSectorVolatilityCycleRequest
{
    public Guid SectorId { get; init; }
    public string CurrentPhase { get; init; } = "stable";
    public float VolatilityIndex { get; init; }
    public DateTime? NextTransitionAt { get; init; }
}
