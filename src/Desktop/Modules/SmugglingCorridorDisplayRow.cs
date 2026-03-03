namespace GalacticTrader.Desktop.Modules;

public sealed class SmugglingCorridorDisplayRow
{
    public required string Corridor { get; init; }
    public required int SmugglingRuns { get; init; }
    public required decimal AverageTradeValue { get; init; }
}
