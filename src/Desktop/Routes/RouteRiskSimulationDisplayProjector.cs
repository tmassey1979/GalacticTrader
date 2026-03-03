namespace GalacticTrader.Desktop.Routes;

public static class RouteRiskSimulationDisplayProjector
{
    public static RouteRiskSimulationDisplaySnapshot Build(RouteRiskSimulation simulation)
    {
        return new RouteRiskSimulationDisplaySnapshot
        {
            RiskBand = simulation.RiskBand,
            RiskBandScore = ResolveBandScore(simulation.RiskBand),
            InterceptionPercent = Math.Round(simulation.InterceptionProbability * 100d, 1, MidpointRounding.AwayFromZero),
            ExpectedRevenue = Math.Round(simulation.ExpectedRevenueProxy, 2, MidpointRounding.AwayFromZero),
            ExpectedLoss = Math.Round(simulation.ExpectedLossProxy, 2, MidpointRounding.AwayFromZero),
            ProtectionCost = Math.Round(simulation.ProtectionCostEstimate, 2, MidpointRounding.AwayFromZero)
        };
    }

    private static double ResolveBandScore(string riskBand)
    {
        return riskBand switch
        {
            "Low" => 25d,
            "Moderate" => 50d,
            "High" => 75d,
            "Critical" => 95d,
            _ => 40d
        };
    }
}
