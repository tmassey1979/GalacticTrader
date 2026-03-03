using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class MarketVolatilityTrendProjectorTests
{
    [Theory]
    [InlineData(18.4, "Cooling", "Low")]
    [InlineData(44.5, "Stable", "Moderate")]
    [InlineData(72.1, "Rising", "High")]
    [InlineData(85.0, "Rising", "Extreme")]
    public void Build_ProjectsDirectionAndBand(double index, string expectedDirection, string expectedBand)
    {
        var projection = MarketVolatilityTrendProjector.Build((decimal)index);

        Assert.Equal(expectedDirection, projection.Direction);
        Assert.Equal(expectedBand, projection.Band);
    }
}
