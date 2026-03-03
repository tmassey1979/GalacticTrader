namespace GalacticTrader.Desktop.Routes;

public sealed class RouteOptimizationDisplayRow
{
    public required string Profile { get; init; }
    public int TravelTimeSeconds { get; init; }
    public double FuelCost { get; init; }
    public double RiskScore { get; init; }
    public double TotalCost { get; init; }
}
