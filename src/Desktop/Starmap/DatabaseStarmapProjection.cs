using GalacticTrader.Desktop.Api;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class DatabaseStarmapProjection
{
    public static IReadOnlyList<StarNode> ToStars(IReadOnlyList<SectorApiDto> sectors)
    {
        return sectors.Select((sector, index) =>
            new StarNode(
                Name: sector.Name,
                Position: new Point3D(sector.X, sector.Y, sector.Z),
                Magnitude: 2 + ((index % 5) * 0.65),
                IsHub: index % 6 == 0))
            .ToList();
    }

    public static IReadOnlyList<RouteSegment> ToRoutes(IReadOnlyList<RouteApiDto> routes, IReadOnlyList<SectorApiDto> sectors)
    {
        var sectorsById = sectors.ToDictionary(sector => sector.Id);

        var projected = new List<RouteSegment>();
        foreach (var route in routes)
        {
            if (!sectorsById.TryGetValue(route.FromSectorId, out var fromSector))
            {
                continue;
            }

            if (!sectorsById.TryGetValue(route.ToSectorId, out var toSector))
            {
                continue;
            }

            projected.Add(new RouteSegment(
                Name: $"{fromSector.Name} -> {toSector.Name}",
                From: new Point3D(fromSector.X, fromSector.Y, fromSector.Z),
                To: new Point3D(toSector.X, toSector.Y, toSector.Z),
                IsHighRisk: route.BaseRiskScore >= 60));
        }

        return projected;
    }
}
