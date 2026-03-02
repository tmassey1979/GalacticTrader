namespace GalacticTrader.Data.Models;

public sealed class TerritoryDominance
{
    public Guid Id { get; set; }
    public Guid FactionId { get; set; }
    public int ControlledSectorCount { get; set; }
    public float InfrastructureControlScore { get; set; }
    public float WarMomentumScore { get; set; }
    public float DominanceScore { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Faction? Faction { get; set; }
}
