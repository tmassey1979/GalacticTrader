using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class RouteVisualStyleResolverTests
{
    [Fact]
    public void Build_ProducesMoreIntenseStyleForHigherRisk()
    {
        var lowStyle = RouteVisualStyleResolver.Build(new RouteSegment(
            "A->B",
            new Point3D(),
            new Point3D(1, 1, 1),
            IsHighRisk: false,
            BaseRiskScore: 10f));
        var highStyle = RouteVisualStyleResolver.Build(new RouteSegment(
            "A->C",
            new Point3D(),
            new Point3D(1, 2, 1),
            IsHighRisk: true,
            BaseRiskScore: 90f));

        Assert.True(highStyle.Width > lowStyle.Width);
        Assert.True(highStyle.Opacity > lowStyle.Opacity);
        Assert.True(highStyle.Color.R > lowStyle.Color.R);
    }
}
