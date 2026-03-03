namespace GalacticTrader.Services.Strategic;

public sealed class SectorVolatilityCycleDto
{
    public Guid Id { get; init; }
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public string CurrentPhase { get; init; } = string.Empty;
    public float VolatilityIndex { get; init; }
    public DateTime CycleStartedAt { get; init; }
    public DateTime NextTransitionAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
