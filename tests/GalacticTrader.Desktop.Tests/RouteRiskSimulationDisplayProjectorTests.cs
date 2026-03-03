using GalacticTrader.Desktop.Routes;

namespace GalacticTrader.Desktop.Tests;

public sealed class RouteRiskSimulationDisplayProjectorTests
{
    [Fact]
    public void Build_ProjectsSimulationForUiDisplay()
    {
        var simulation = new RouteRiskSimulation
        {
            InterceptionProbability = 0.642,
            ExpectedCostProxy = 1000,
            ExpectedRevenueProxy = 820.445,
            ExpectedLossProxy = 205.54,
            ProtectionCostEstimate = 184.665,
            RiskBand = "High"
        };

        var display = RouteRiskSimulationDisplayProjector.Build(simulation);

        Assert.Equal("High", display.RiskBand);
        Assert.Equal(75d, display.RiskBandScore);
        Assert.Equal(64.2d, display.InterceptionPercent);
        Assert.Equal(820.45d, display.ExpectedRevenue);
        Assert.Equal(205.54d, display.ExpectedLoss);
        Assert.Equal(184.67d, display.ProtectionCost);
    }
}
