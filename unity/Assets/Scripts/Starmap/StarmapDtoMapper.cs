using GalacticTrader.ClientSdk.Starmap;
using GalacticTrader.Desktop.Api;
using System.Collections.Generic;
using System.Linq;

namespace GalacticTrader.Unity.Starmap;

public static class StarmapDtoMapper
{
    public static IReadOnlyList<StarmapSectorNode> ToSectorNodes(IReadOnlyList<SectorApiDto> sectors)
    {
        return sectors
            .Select(static sector => new StarmapSectorNode(
                sector.Id,
                sector.Name,
                new MapPoint3(sector.X, sector.Y, sector.Z),
                IsHub: false))
            .ToArray();
    }

    public static IReadOnlyList<StarmapRouteEdge> ToRouteEdges(IReadOnlyList<RouteApiDto> routes)
    {
        return routes
            .Select(static route => new StarmapRouteEdge(
                route.Id,
                route.FromSectorId,
                route.ToSectorId,
                route.BaseRiskScore >= 70d))
            .ToArray();
    }
}
