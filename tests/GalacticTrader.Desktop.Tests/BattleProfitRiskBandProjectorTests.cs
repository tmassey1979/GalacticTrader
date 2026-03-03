using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class BattleProfitRiskBandProjectorTests
{
    [Theory]
    [InlineData(8.2, "Efficient")]
    [InlineData(28.4, "Balanced")]
    [InlineData(67.1, "Exposed")]
    [InlineData(95.0, "Critical Exposure")]
    public void Build_MapsRatioToExpectedBand(double ratio, string expected)
    {
        var band = BattleProfitRiskBandProjector.Build((decimal)ratio);

        Assert.Equal(expected, band);
    }
}
