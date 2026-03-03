namespace GalacticTrader.Desktop.Fleet;

public sealed class FleetRoutePerformanceEntry
{
    public string RunLabel { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public long Quantity { get; init; }
    public decimal GrossValue { get; init; }
    public decimal Tariff { get; init; }
    public decimal NetValue { get; init; }
    public string Status { get; init; } = string.Empty;
}
