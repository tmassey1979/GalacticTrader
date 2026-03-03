namespace GalacticTrader.Services.Strategic;

public sealed class PublishIntelligenceReportRequest
{
    public Guid NetworkId { get; init; }
    public Guid SectorId { get; init; }
    public string SignalType { get; init; } = string.Empty;
    public float ConfidenceScore { get; init; }
    public string Payload { get; init; } = string.Empty;
    public int TtlMinutes { get; init; } = 30;
}
