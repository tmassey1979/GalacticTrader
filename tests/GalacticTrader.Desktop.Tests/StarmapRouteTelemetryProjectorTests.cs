using GalacticTrader.Desktop.Starmap;

namespace GalacticTrader.Desktop.Tests;

public sealed class StarmapRouteTelemetryProjectorTests
{
    [Fact]
    public void Build_ProjectsRiskDensityAndPirateProbability()
    {
        var projection = StarmapRouteTelemetryProjector.Build(72.4f);

        Assert.Equal(72.4f, projection.BaseRiskScore);
        Assert.InRange(projection.EconomicDensity, 8f, 95f);
        Assert.InRange(projection.PiratePresenceProbability, 4f, 95f);
    }

    [Fact]
    public void Build_ClampsOutOfRangeRiskScores()
    {
        var low = StarmapRouteTelemetryProjector.Build(-20f);
        var high = StarmapRouteTelemetryProjector.Build(200f);

        Assert.Equal(0f, low.BaseRiskScore);
        Assert.Equal(100f, high.BaseRiskScore);
    }
}
