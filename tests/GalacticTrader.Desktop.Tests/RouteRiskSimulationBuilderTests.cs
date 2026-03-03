using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Routes;

namespace GalacticTrader.Desktop.Tests;

public sealed class RouteRiskSimulationBuilderTests
{
    [Fact]
    public void Build_ComputesExpectedRiskMetrics()
    {
        var plan = new RoutePlanApiDto
        {
            TotalCost = 800d,
            TotalFuelCost = 200d,
            TotalRiskScore = 64d
        };

        var simulation = RouteRiskSimulationBuilder.Build(plan);

        Assert.Equal(0.64d, simulation.InterceptionProbability, precision: 3);
        Assert.Equal(1000d, simulation.ExpectedCostProxy, precision: 3);
        Assert.Equal(224d, simulation.ExpectedLossProxy, precision: 3);
        Assert.Equal(200d, simulation.ProtectionCostEstimate, precision: 3);
        Assert.Equal("High", simulation.RiskBand);
    }

    [Fact]
    public void Build_ClampsRiskToCriticalRange()
    {
        var plan = new RoutePlanApiDto
        {
            TotalCost = 200d,
            TotalFuelCost = 0d,
            TotalRiskScore = 250d
        };

        var simulation = RouteRiskSimulationBuilder.Build(plan);

        Assert.Equal(1d, simulation.InterceptionProbability, precision: 3);
        Assert.Equal(58d, simulation.ProtectionCostEstimate, precision: 3);
        Assert.Equal("Critical", simulation.RiskBand);
    }
}
