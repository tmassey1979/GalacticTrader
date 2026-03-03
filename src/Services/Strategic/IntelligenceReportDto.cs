namespace GalacticTrader.Services.Strategic;

public sealed class IntelligenceReportDto
{
    public Guid Id { get; init; }
    public Guid NetworkId { get; init; }
    public string NetworkName { get; init; } = string.Empty;
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public string SignalType { get; init; } = string.Empty;
    public float ConfidenceScore { get; init; }
    public string Payload { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
}
