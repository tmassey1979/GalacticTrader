namespace GalacticTrader.Data.Models;

public sealed class SectorVolatilityCycle
{
    public Guid Id { get; set; }
    public Guid SectorId { get; set; }
    public string CurrentPhase { get; set; } = "stable";
    public float VolatilityIndex { get; set; }
    public DateTime CycleStartedAt { get; set; }
    public DateTime NextTransitionAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public Sector? Sector { get; set; }
}
