namespace GalacticTrader.Desktop.Api;

public sealed class SmugglingCorridorInsightApiDto
{
    public string FromSectorName { get; init; } = string.Empty;
    public string ToSectorName { get; init; } = string.Empty;
    public int SmugglingRuns { get; init; }
    public decimal AverageTradeValue { get; init; }
}
