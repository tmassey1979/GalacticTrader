namespace GalacticTrader.Desktop.Dashboard;

public sealed class StatusMetricSnapshot
{
    public decimal LiquidCredits { get; init; }
    public int ReputationScore { get; init; }
    public int FleetStrength { get; init; }
    public int ActiveRoutes { get; init; }
    public int AlertCount { get; init; }
}
