namespace GalacticTrader.Data.Models;

public sealed class InfrastructureOwnership
{
    public Guid Id { get; set; }
    public Guid SectorId { get; set; }
    public Guid FactionId { get; set; }
    public string InfrastructureType { get; set; } = string.Empty;
    public float ControlScore { get; set; }
    public DateTime ClaimedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public Sector? Sector { get; set; }
    public Faction? Faction { get; set; }
}
