namespace GalacticTrader.Desktop.Routes;

public sealed class RouteRiskSimulationDisplaySnapshot
{
    public required string RiskBand { get; init; }
    public double RiskBandScore { get; init; }
    public double InterceptionPercent { get; init; }
    public double ExpectedRevenue { get; init; }
    public double ExpectedLoss { get; init; }
    public double ProtectionCost { get; init; }
}
