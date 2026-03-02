namespace GalacticTrader.Desktop.Starmap;

public static class RouteNetworkBuilder
{
    public static IReadOnlyList<RouteSegment> CreateRoutes(IReadOnlyList<StarNode> stars)
    {
        var routes = new List<RouteSegment>();
        var seenPairs = new HashSet<string>(StringComparer.Ordinal);
        var random = new Random(7319);

        for (var i = 0; i < stars.Count; i++)
        {
            var current = stars[i];
            var nearest = stars
                .Where(s => !ReferenceEquals(s, current))
                .OrderBy(s => (s.Position - current.Position).LengthSquared)
                .Take(2)
                .ToList();

            foreach (var neighbor in nearest)
            {
                var pairKey = string.Compare(current.Name, neighbor.Name, StringComparison.Ordinal) < 0
                    ? $"{current.Name}:{neighbor.Name}"
                    : $"{neighbor.Name}:{current.Name}";

                if (!seenPairs.Add(pairKey))
                {
                    continue;
                }

                var distance = (neighbor.Position - current.Position).Length;
                var isHighRisk = distance > 52 || random.NextDouble() > 0.72;
                routes.Add(new RouteSegment(
                    Name: $"{current.Name} -> {neighbor.Name}",
                    From: current.Position,
                    To: neighbor.Position,
                    IsHighRisk: isHighRisk));
            }
        }

        return routes
            .OrderByDescending(route => (route.To - route.From).Length)
            .Take(18)
            .ToList();
    }
}
