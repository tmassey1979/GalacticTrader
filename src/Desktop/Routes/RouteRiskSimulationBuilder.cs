using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Routes;

public static class RouteRiskSimulationBuilder
{
    public static RouteRiskSimulation Build(RoutePlanApiDto plan)
    {
        var riskScore = Math.Clamp(plan.TotalRiskScore, 0d, 100d);
        var interceptionProbability = riskScore / 100d;
        var expectedCost = Math.Max(0d, plan.TotalCost + plan.TotalFuelCost);
        var marginFactor = 1.25d - (riskScore / 220d);
        var expectedRevenue = expectedCost * Math.Max(0.2d, marginFactor);
        var expectedLoss = expectedCost * interceptionProbability * 0.35d;
        var protectionRate = 0.04d + (riskScore / 400d);
        var protectionCostEstimate = expectedCost * protectionRate;

        var riskBand = riskScore switch
        {
            < 30d => "Low",
            < 60d => "Moderate",
            < 80d => "High",
            _ => "Critical"
        };

        return new RouteRiskSimulation
        {
            InterceptionProbability = interceptionProbability,
            ExpectedCostProxy = expectedCost,
            ExpectedRevenueProxy = expectedRevenue,
            ExpectedLossProxy = expectedLoss,
            ProtectionCostEstimate = protectionCostEstimate,
            RiskBand = riskBand
        };
    }
}
