namespace GalacticTrader.Data.Models;

public sealed class IntelligenceNetwork
{
    public Guid Id { get; set; }
    public Guid OwnerPlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssetCount { get; set; }
    public float CoverageScore { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Player? OwnerPlayer { get; set; }
    public ICollection<IntelligenceReport> Reports { get; set; } = new List<IntelligenceReport>();
}
