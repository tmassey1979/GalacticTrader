using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class StarmapSceneBuilder
{
    public static StarmapScene Build()
    {
        var stars = StarCatalogBuilder.CreateStars();
        var routes = RouteNetworkBuilder.CreateRoutes(stars);
        return Build(stars, routes);
    }

    public static StarmapScene Build(IReadOnlyList<StarNode> stars, IReadOnlyList<RouteSegment> routes)
    {
        var scene = new Model3DGroup();
        foreach (var route in routes)
        {
            scene.Children.Add(RouteModelFactory.Create(route));
        }

        foreach (var star in stars)
        {
            scene.Children.Add(StarModelFactory.Create(star));
        }

        return new StarmapScene(stars, routes, scene);
    }
}
