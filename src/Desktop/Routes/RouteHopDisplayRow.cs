namespace GalacticTrader.Desktop.Routes;

public sealed class RouteHopDisplayRow
{
    public required string Segment { get; init; }
    public int TravelTimeSeconds { get; init; }
    public float FuelCost { get; init; }
    public float RiskScore { get; init; }
    public float EconomicDensity { get; init; }
    public float PiratePresenceProbability { get; init; }
}
