namespace GalacticTrader.ClientSdk.Starmap;

public readonly record struct StarmapChunkingOptions(
    double ChunkSize,
    int BaseChunkRadius)
{
    public static StarmapChunkingOptions StartupDefault { get; } = new(
        ChunkSize: 80d,
        BaseChunkRadius: 2);

    public StarmapChunkKey ResolveChunk(MapPoint3 point)
    {
        return new StarmapChunkKey(
            X: ToChunkIndex(point.X),
            Y: ToChunkIndex(point.Y),
            Z: ToChunkIndex(point.Z));
    }

    public IReadOnlyList<StarmapChunkKey> ResolveWindow(StarmapChunkKey center, int radius)
    {
        var boundedRadius = Math.Max(0, radius);
        var keys = new List<StarmapChunkKey>();
        for (var x = center.X - boundedRadius; x <= center.X + boundedRadius; x++)
        {
            for (var y = center.Y - boundedRadius; y <= center.Y + boundedRadius; y++)
            {
                for (var z = center.Z - boundedRadius; z <= center.Z + boundedRadius; z++)
                {
                    keys.Add(new StarmapChunkKey(x, y, z));
                }
            }
        }

        return keys;
    }

    private int ToChunkIndex(double coordinate)
    {
        if (ChunkSize <= 0d)
        {
            return 0;
        }

        return (int)Math.Floor(coordinate / ChunkSize);
    }
}
