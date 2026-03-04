namespace GalacticTrader.ClientSdk.Starmap;

public sealed class StarmapChunkIndex
{
    private readonly Dictionary<StarmapChunkKey, IReadOnlyList<StarmapSectorNode>> _sectorsByChunk;

    private StarmapChunkIndex(
        IReadOnlyDictionary<Guid, StarmapSectorNode> sectorsById,
        IReadOnlyList<StarmapRouteEdge> routes,
        Dictionary<StarmapChunkKey, IReadOnlyList<StarmapSectorNode>> sectorsByChunk,
        StarmapChunkingOptions chunkingOptions)
    {
        SectorsById = sectorsById;
        Routes = routes;
        _sectorsByChunk = sectorsByChunk;
        ChunkingOptions = chunkingOptions;
    }

    public IReadOnlyDictionary<Guid, StarmapSectorNode> SectorsById { get; }

    public IReadOnlyList<StarmapRouteEdge> Routes { get; }

    public StarmapChunkingOptions ChunkingOptions { get; }

    public static StarmapChunkIndex Build(
        IReadOnlyList<StarmapSectorNode> sectors,
        IReadOnlyList<StarmapRouteEdge> routes,
        StarmapChunkingOptions? chunkingOptions = null)
    {
        var resolvedChunking = chunkingOptions ?? StarmapChunkingOptions.StartupDefault;
        var sectorsById = sectors.ToDictionary(static sector => sector.SectorId);
        var grouped = sectors
            .GroupBy(sector => resolvedChunking.ResolveChunk(sector.Position))
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<StarmapSectorNode>)group.ToArray());

        return new StarmapChunkIndex(
            sectorsById,
            routes.ToArray(),
            grouped,
            resolvedChunking);
    }

    public bool HasChunk(StarmapChunkKey key)
    {
        return _sectorsByChunk.ContainsKey(key);
    }

    public IReadOnlyList<StarmapSectorNode> GetSectorsInChunk(StarmapChunkKey key)
    {
        return _sectorsByChunk.TryGetValue(key, out var sectors)
            ? sectors
            : [];
    }
}
