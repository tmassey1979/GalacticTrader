namespace GalacticTrader.Services.Telemetry;

public sealed class SmugglingCorridorInsightDto
{
    public string FromSectorName { get; init; } = string.Empty;
    public string ToSectorName { get; init; } = string.Empty;
    public int SmugglingRuns { get; init; }
    public decimal AverageTradeValue { get; init; }
}
