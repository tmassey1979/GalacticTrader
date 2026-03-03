using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class CashFlowSparklineBuilderTests
{
    [Fact]
    public void Build_ReturnsTwoMidlinePoints_WhenSingleValue()
    {
        var points = CashFlowSparklineBuilder.Build([1000m], width: 120, height: 30);

        Assert.Equal(2, points.Count);
        Assert.Equal(0, points[0].X);
        Assert.Equal(15, points[0].Y);
        Assert.Equal(120, points[1].X);
        Assert.Equal(15, points[1].Y);
    }

    [Fact]
    public void Build_ProjectsTrendWithinBounds_ForMultiValueSeries()
    {
        var points = CashFlowSparklineBuilder.Build([100m, 160m, 130m], width: 100, height: 40);

        Assert.Equal(3, points.Count);
        Assert.Equal(0, points[0].X);
        Assert.Equal(50, points[1].X);
        Assert.Equal(100, points[2].X);
        Assert.All(points, point =>
        {
            Assert.InRange(point.Y, 0, 40);
        });
    }
}
