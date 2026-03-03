using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Routes;

namespace GalacticTrader.Desktop.Tests;

public sealed class RouteOverlayProjectorTests
{
    [Fact]
    public void Build_ProjectsDensityAndPiratePresence_FromHopSignals()
    {
        var hop = new RouteHopApiDto
        {
            BaseRiskScore = 42f,
            BaseFuelCost = 28f,
            BaseTravelTimeSeconds = 180
        };

        var overlay = RouteOverlayProjector.Build(hop);

        Assert.Equal(54.2f, overlay.EconomicDensity);
        Assert.Equal(47f, overlay.PiratePresenceProbability);
    }

    [Fact]
    public void Build_ClampsOverlayValuesToBounds()
    {
        var hop = new RouteHopApiDto
        {
            BaseRiskScore = 160f,
            BaseFuelCost = 400f,
            BaseTravelTimeSeconds = 6000
        };

        var overlay = RouteOverlayProjector.Build(hop);

        Assert.Equal(0f, overlay.EconomicDensity);
        Assert.Equal(100f, overlay.PiratePresenceProbability);
    }
}
