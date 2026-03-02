namespace GalacticTrader.MapGenerator.Generation;

public sealed class MapLayoutGenerator
{
    public GeneratedMapLayout Generate(int seed, int sectorCount, int routeDensity)
    {
        var normalizedSectorCount = Math.Clamp(sectorCount, 4, 160);
        var normalizedRouteDensity = Math.Clamp(routeDensity, 1, 4);
        var random = new Random(seed);

        var sectors = new List<GeneratedSector>(normalizedSectorCount);
        for (var index = 0; index < normalizedSectorCount; index++)
        {
            var name = BuildSectorName(index, random);
            var angle = (2 * Math.PI * index) / normalizedSectorCount;
            var radius = 60 + random.Next(0, 360);
            var x = (float)(Math.Cos(angle) * radius);
            var y = (float)(random.NextDouble() * 130 - 65);
            var z = (float)(Math.Sin(angle) * radius);
            sectors.Add(new GeneratedSector(index, name, x, y, z));
        }

        var routes = new List<GeneratedRoute>();
        var uniqueEdges = new HashSet<(int A, int B)>();

        // Build at least one spanning loop to keep the generated graph navigable.
        for (var index = 0; index < normalizedSectorCount; index++)
        {
            var from = index;
            var to = (index + 1) % normalizedSectorCount;
            AddRoute(uniqueEdges, routes, from, to, isHighRisk: false);
        }

        var targetRouteCount = normalizedSectorCount * normalizedRouteDensity;
        while (routes.Count < targetRouteCount)
        {
            var from = random.Next(0, normalizedSectorCount);
            var to = random.Next(0, normalizedSectorCount);
            if (from == to)
            {
                continue;
            }

            var highRisk = random.Next(0, 100) < 30;
            AddRoute(uniqueEdges, routes, from, to, highRisk);
        }

        return new GeneratedMapLayout(sectors, routes);
    }

    private static string BuildSectorName(int index, Random random)
    {
        var prefixes = new[] { "Sol", "Vega", "Astra", "Helios", "Orion", "Nyx", "Draco", "Atlas" };
        var suffixes = new[] { "Prime", "Reach", "Gate", "Harbor", "Forge", "Bastion", "Crown", "Relay" };
        var prefix = prefixes[random.Next(prefixes.Length)];
        var suffix = suffixes[random.Next(suffixes.Length)];
        return $"{prefix} {suffix} {index + 1}";
    }

    private static void AddRoute(
        HashSet<(int A, int B)> uniqueEdges,
        List<GeneratedRoute> routes,
        int from,
        int to,
        bool isHighRisk)
    {
        var key = from < to ? (from, to) : (to, from);
        if (!uniqueEdges.Add(key))
        {
            return;
        }

        routes.Add(new GeneratedRoute(from, to, isHighRisk));
    }
}
