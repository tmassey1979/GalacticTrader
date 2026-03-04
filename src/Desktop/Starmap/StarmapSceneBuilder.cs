using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class StarmapSceneBuilder
{
    public static StarmapScene Build()
    {
        var stars = StarCatalogBuilder.CreateStars();
        var routes = RouteNetworkBuilder.CreateRoutes(stars);
        return Build(stars, routes, StarmapRenderBudget.Unlimited);
    }

    public static StarmapScene BuildMetadataOnly()
    {
        var stars = StarCatalogBuilder.CreateStars();
        var routes = RouteNetworkBuilder.CreateRoutes(stars);
        return Build(stars, routes, StarmapRenderBudget.Unlimited, include3DModels: false);
    }

    public static StarmapScene Build(IReadOnlyList<StarNode> stars, IReadOnlyList<RouteSegment> routes)
    {
        return Build(stars, routes, StarmapRenderBudget.Unlimited);
    }

    public static StarmapScene Build(
        IReadOnlyList<StarNode> stars,
        IReadOnlyList<RouteSegment> routes,
        StarmapRenderBudget renderBudget,
        bool include3DModels = true)
    {
        if (!include3DModels)
        {
            return new StarmapScene(stars, routes, new Model3DGroup());
        }

        var scene = new Model3DGroup();
        var renderCenter = ResolveRenderCenter(stars);

        var renderedStars = SelectStarsForRender(stars, renderCenter, renderBudget.MaxRenderedStars)
            .ToList();
        var renderedStarKeys = renderedStars
            .Select(static star => Point3DKey.FromPoint(star.Position))
            .ToHashSet();
        var renderedRoutes = SelectRoutesForRender(
            routes,
            renderCenter,
            renderedStarKeys,
            renderBudget.MaxRenderedRoutes);

        foreach (var route in renderedRoutes)
        {
            scene.Children.Add(RouteModelFactory.Create(route));
        }

        foreach (var star in renderedStars)
        {
            scene.Children.Add(StarModelFactory.Create(star));
        }

        return new StarmapScene(stars, routes, scene);
    }

    private static Point3D ResolveRenderCenter(IReadOnlyList<StarNode> stars)
    {
        if (stars.Count == 0)
        {
            return new Point3D(0, 0, 0);
        }

        var sumX = 0d;
        var sumY = 0d;
        var sumZ = 0d;

        foreach (var star in stars)
        {
            sumX += star.Position.X;
            sumY += star.Position.Y;
            sumZ += star.Position.Z;
        }

        return new Point3D(
            sumX / stars.Count,
            sumY / stars.Count,
            sumZ / stars.Count);
    }

    private static IEnumerable<StarNode> SelectStarsForRender(
        IReadOnlyList<StarNode> stars,
        Point3D renderCenter,
        int maxRenderedStars)
    {
        if (maxRenderedStars <= 0 || stars.Count <= maxRenderedStars)
        {
            return stars;
        }

        return stars
            .OrderByDescending(static star => star.IsHub)
            .ThenBy(star => DistanceSquared(star.Position, renderCenter))
            .ThenBy(static star => star.Name, StringComparer.Ordinal)
            .Take(maxRenderedStars)
            .ToList();
    }

    private static IReadOnlyList<RouteSegment> SelectRoutesForRender(
        IReadOnlyList<RouteSegment> routes,
        Point3D renderCenter,
        HashSet<Point3DKey> renderedStarKeys,
        int maxRenderedRoutes)
    {
        if (maxRenderedRoutes <= 0 || routes.Count <= maxRenderedRoutes)
        {
            return routes;
        }

        var routesForRenderedStars = routes
            .Where(route =>
                renderedStarKeys.Contains(Point3DKey.FromPoint(route.From)) &&
                renderedStarKeys.Contains(Point3DKey.FromPoint(route.To)))
            .ToList();

        var routePool = routesForRenderedStars.Count > 0
            ? routesForRenderedStars
            : routes;

        return routePool
            .OrderBy(route => DistanceSquared(GetRouteMidpoint(route), renderCenter))
            .ThenBy(static route => route.Name, StringComparer.Ordinal)
            .Take(maxRenderedRoutes)
            .ToList();
    }

    private static Point3D GetRouteMidpoint(RouteSegment route)
    {
        return new Point3D(
            (route.From.X + route.To.X) / 2,
            (route.From.Y + route.To.Y) / 2,
            (route.From.Z + route.To.Z) / 2);
    }

    private static double DistanceSquared(Point3D first, Point3D second)
    {
        var deltaX = first.X - second.X;
        var deltaY = first.Y - second.Y;
        var deltaZ = first.Z - second.Z;
        return (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
    }

    private readonly record struct Point3DKey(long X, long Y, long Z)
    {
        public static Point3DKey FromPoint(Point3D point)
        {
            return new Point3DKey(
                ToScaled(point.X),
                ToScaled(point.Y),
                ToScaled(point.Z));
        }

        private static long ToScaled(double value)
        {
            return (long)Math.Round(value * 1000d);
        }
    }
}
