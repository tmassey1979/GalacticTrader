using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class SmugglingRiskIndicatorBuilderTests
{
    [Theory]
    [InlineData(0.05f, 0.0f, 0.0f, 1.00f, "Low")]
    [InlineData(0.45f, 0.2f, 0.1f, 1.05f, "Moderate")]
    [InlineData(0.85f, 0.5f, 0.4f, 1.30f, "High")]
    [InlineData(1.60f, 1.0f, 1.0f, 2.00f, "Critical")]
    public void Build_ReturnsExpectedBand(float risk, float pirate, float monopoly, float demand, string expectedBand)
    {
        var indicator = SmugglingRiskIndicatorBuilder.Build(risk, pirate, monopoly, demand);

        Assert.Equal(expectedBand, indicator.Band);
        Assert.InRange(indicator.Score, 0f, 100f);
    }

    [Fact]
    public void Build_ClampsScoreToMaximum()
    {
        var indicator = SmugglingRiskIndicatorBuilder.Build(10f, 10f, 10f, 5f);

        Assert.Equal(100f, indicator.Score);
        Assert.Equal("Critical", indicator.Band);
    }
}
