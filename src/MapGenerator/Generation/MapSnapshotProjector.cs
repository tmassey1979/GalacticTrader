using GalacticTrader.MapGenerator.Api;

namespace GalacticTrader.MapGenerator.Generation;

public static class MapSnapshotProjector
{
    public static IReadOnlyList<string> BuildSectorRows(IReadOnlyList<SectorApiDto> sectors)
    {
        return sectors
            .OrderBy(static sector => sector.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static sector => sector.Id)
            .Select(static sector => $"{sector.Name} [{sector.X:F1}, {sector.Y:F1}, {sector.Z:F1}]")
            .ToArray();
    }

    public static IReadOnlyList<string> BuildRouteRows(IReadOnlyList<RouteApiDto> routes)
    {
        return routes
            .OrderByDescending(static route => route.BaseRiskScore)
            .ThenBy(static route => route.FromSectorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.ToSectorName, StringComparer.OrdinalIgnoreCase)
            .Select(static route => $"{route.FromSectorName} -> {route.ToSectorName} (Risk {route.BaseRiskScore:N1})")
            .ToArray();
    }
}
