namespace GalacticTrader.Data.Models;

public sealed class IntelligenceReport
{
    public Guid Id { get; set; }
    public Guid NetworkId { get; set; }
    public Guid SectorId { get; set; }
    public string SignalType { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }

    public IntelligenceNetwork? Network { get; set; }
    public Sector? Sector { get; set; }
}
