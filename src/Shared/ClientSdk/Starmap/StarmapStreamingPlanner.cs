namespace GalacticTrader.ClientSdk.Starmap;

public sealed class StarmapStreamingPlanner
{
    private readonly StarmapChunkIndex _index;
    private readonly StarmapRenderBudget _renderBudget;
    private readonly StarmapLodBands _lodBands;

    public StarmapStreamingPlanner(
        StarmapChunkIndex index,
        StarmapRenderBudget? renderBudget = null,
        StarmapLodBands? lodBands = null)
    {
        _index = index;
        _renderBudget = renderBudget ?? StarmapRenderBudget.StartupDefault;
        _lodBands = lodBands ?? StarmapLodBands.StartupDefault;
    }

    public StarmapFramePlan PlanFrame(StarmapCameraState camera)
    {
        var centerChunk = _index.ChunkingOptions.ResolveChunk(camera.Position);
        var activeChunks = ResolveActiveChunks(centerChunk, camera.ViewDistance);

        var candidateSectors = activeChunks
            .SelectMany(_index.GetSectorsInChunk)
            .DistinctBy(static sector => sector.SectorId)
            .ToArray();

        var orderedSectors = candidateSectors
            .Select(sector => new
            {
                Sector = sector,
                Distance = Math.Sqrt(sector.Position.DistanceSquaredTo(camera.Position))
            })
            .OrderByDescending(static entry => entry.Sector.IsHub)
            .ThenBy(static entry => entry.Distance)
            .ThenBy(static entry => entry.Sector.Name, StringComparer.Ordinal)
            .ToArray();

        var wasSectorBudgetApplied = orderedSectors.Length > _renderBudget.MaxRenderedSectors;
        var renderedSectors = orderedSectors
            .Take(Math.Max(0, _renderBudget.MaxRenderedSectors))
            .Select(entry => new StarmapSectorRenderPlan(
                entry.Sector.SectorId,
                entry.Sector.Name,
                entry.Sector.Position,
                _lodBands.Resolve(entry.Distance),
                entry.Distance,
                entry.Sector.IsHub))
            .ToArray();

        var renderedSectorIds = renderedSectors
            .Select(static sector => sector.SectorId)
            .ToHashSet();

        var candidateRoutes = _index.Routes
            .Where(route => renderedSectorIds.Contains(route.FromSectorId) && renderedSectorIds.Contains(route.ToSectorId))
            .Select(route => CreateRouteRenderPlan(route, camera.Position))
            .Where(static route => route is not null)
            .Select(static route => route!)
            .OrderBy(static route => route.DistanceFromCamera)
            .ThenBy(static route => route.RouteId)
            .ToArray();

        var wasRouteBudgetApplied = candidateRoutes.Length > _renderBudget.MaxRenderedRoutes;
        var renderedRoutes = candidateRoutes
            .Take(Math.Max(0, _renderBudget.MaxRenderedRoutes))
            .ToArray();

        return new StarmapFramePlan(
            centerChunk,
            activeChunks,
            renderedSectors,
            renderedRoutes,
            wasSectorBudgetApplied,
            wasRouteBudgetApplied);
    }

    private IReadOnlyList<StarmapChunkKey> ResolveActiveChunks(StarmapChunkKey centerChunk, double viewDistance)
    {
        var distanceBasedRadius = _index.ChunkingOptions.ChunkSize <= 0d
            ? 0
            : (int)Math.Ceiling(Math.Max(0d, viewDistance) / _index.ChunkingOptions.ChunkSize);

        var resolvedRadius = Math.Max(_index.ChunkingOptions.BaseChunkRadius, distanceBasedRadius);

        var chunkWindow = _index.ChunkingOptions.ResolveWindow(centerChunk, resolvedRadius);
        var sorted = chunkWindow
            .OrderBy(key => ChunkDistanceSquared(centerChunk, key))
            .ThenBy(static key => key.X)
            .ThenBy(static key => key.Y)
            .ThenBy(static key => key.Z)
            .ToArray();

        var existingChunks = sorted
            .Where(_index.HasChunk)
            .ToArray();

        var candidateChunks = existingChunks.Length > 0
            ? existingChunks
            : sorted;

        var capped = candidateChunks
            .Take(Math.Max(1, _renderBudget.MaxActiveChunks))
            .ToArray();

        return capped;
    }

    private StarmapRouteRenderPlan? CreateRouteRenderPlan(StarmapRouteEdge route, MapPoint3 cameraPosition)
    {
        if (!_index.SectorsById.TryGetValue(route.FromSectorId, out var fromSector) ||
            !_index.SectorsById.TryGetValue(route.ToSectorId, out var toSector))
        {
            return null;
        }

        var midpoint = new MapPoint3(
            X: (fromSector.Position.X + toSector.Position.X) / 2d,
            Y: (fromSector.Position.Y + toSector.Position.Y) / 2d,
            Z: (fromSector.Position.Z + toSector.Position.Z) / 2d);

        var distance = Math.Sqrt(midpoint.DistanceSquaredTo(cameraPosition));
        return new StarmapRouteRenderPlan(
            route.RouteId,
            route.FromSectorId,
            route.ToSectorId,
            _lodBands.Resolve(distance),
            distance,
            route.IsHighRisk);
    }

    private static int ChunkDistanceSquared(StarmapChunkKey first, StarmapChunkKey second)
    {
        var deltaX = first.X - second.X;
        var deltaY = first.Y - second.Y;
        var deltaZ = first.Z - second.Z;
        return (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
    }
}
