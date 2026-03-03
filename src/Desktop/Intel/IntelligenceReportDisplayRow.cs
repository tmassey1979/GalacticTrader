namespace GalacticTrader.Desktop.Intel;

public sealed class IntelligenceReportDisplayRow
{
    public required string SignalType { get; init; }
    public required string SectorName { get; init; }
    public float ConfidenceScore { get; init; }
    public required string Payload { get; init; }
    public required string ExpiresAtUtc { get; init; }
}
