using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class StarmapSceneRenderBudgetTests
{
    [Fact]
    public void Build_WithRenderBudget_BoundsRenderedModelCount()
    {
        var stars = Enumerable.Range(0, 30)
            .Select(index => new StarNode(
                Name: $"S-{index:D2}",
                Position: new Point3D(index * 100, 0, 0),
                Magnitude: 2.5,
                IsHub: index % 5 == 0))
            .ToList();

        var routes = Enumerable.Range(0, stars.Count - 1)
            .Select(index => new RouteSegment(
                Name: $"{stars[index].Name}->{stars[index + 1].Name}",
                From: stars[index].Position,
                To: stars[index + 1].Position,
                IsHighRisk: false))
            .ToList();

        var scene = StarmapSceneBuilder.Build(
            stars,
            routes,
            new StarmapRenderBudget(MaxRenderedStars: 8, MaxRenderedRoutes: 5));

        Assert.Equal(30, scene.Stars.Count);
        Assert.Equal(29, scene.Routes.Count);
        Assert.InRange(scene.Models.Children.Count, 8, 13);

        var renderedRouteCount = scene.Models.Children.Count - 8;
        Assert.InRange(renderedRouteCount, 0, 5);
    }

    [Fact]
    public void Build_WithUnlimitedBudget_RendersAllModels()
    {
        var stars = new[]
        {
            new StarNode("A", new Point3D(0, 0, 0), 2.0, false),
            new StarNode("B", new Point3D(10, 0, 0), 2.0, false),
            new StarNode("C", new Point3D(20, 0, 0), 2.0, false)
        };

        var routes = new[]
        {
            new RouteSegment("A-B", stars[0].Position, stars[1].Position, false),
            new RouteSegment("B-C", stars[1].Position, stars[2].Position, false)
        };

        var scene = StarmapSceneBuilder.Build(stars, routes, StarmapRenderBudget.Unlimited);

        Assert.Equal(5, scene.Models.Children.Count);
    }

    [Fact]
    public void Build_When3DDisabled_PreservesMetadataAndSkipsModelCreation()
    {
        var stars = new[]
        {
            new StarNode("A", new Point3D(0, 0, 0), 2.0, false),
            new StarNode("B", new Point3D(10, 0, 0), 2.0, false)
        };

        var routes = new[]
        {
            new RouteSegment("A-B", stars[0].Position, stars[1].Position, false)
        };

        var scene = StarmapSceneBuilder.Build(
            stars,
            routes,
            new StarmapRenderBudget(MaxRenderedStars: 1, MaxRenderedRoutes: 1),
            include3DModels: false);

        Assert.Equal(2, scene.Stars.Count);
        Assert.Single(scene.Routes);
        Assert.Empty(scene.Models.Children);
    }
}
