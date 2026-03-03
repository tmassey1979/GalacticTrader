namespace GalacticTrader.Desktop.Intel;

public sealed class ThreatAlert
{
    public required string Source { get; init; }
    public required string Headline { get; init; }
    public required string Detail { get; init; }
    public float Severity { get; init; }
}
