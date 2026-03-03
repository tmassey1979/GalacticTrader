namespace GalacticTrader.Desktop.Routes;

public sealed class RouteRiskSimulation
{
    public double InterceptionProbability { get; init; }
    public double ExpectedCostProxy { get; init; }
    public double ExpectedRevenueProxy { get; init; }
    public double ExpectedLossProxy { get; init; }
    public double ProtectionCostEstimate { get; init; }
    public required string RiskBand { get; init; }
}
