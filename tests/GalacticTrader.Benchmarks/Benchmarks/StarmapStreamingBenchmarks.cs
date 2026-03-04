namespace GalacticTrader.Benchmarks;

using BenchmarkDotNet.Attributes;
using GalacticTrader.ClientSdk.Starmap;

[MemoryDiagnoser]
public class StarmapStreamingBenchmarks
{
    private StarmapStreamingPlanner _planner = null!;
    private StarmapCameraState _camera;

    [Params(2000, 8000)]
    public int SectorCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var sectors = BuildSectors(SectorCount);
        var routes = BuildRoutes(sectors);
        var index = StarmapChunkIndex.Build(
            sectors,
            routes,
            new StarmapChunkingOptions(ChunkSize: 80d, BaseChunkRadius: 3));

        _planner = new StarmapStreamingPlanner(
            index,
            new StarmapRenderBudget(MaxRenderedSectors: 600, MaxRenderedRoutes: 1400, MaxActiveChunks: 180),
            new StarmapLodBands(NearDistance: 90d, MidDistance: 260d));
        _camera = new StarmapCameraState(
            Position: new MapPoint3(0d, 0d, 0d),
            ViewDistance: 450d,
            Forward: new MapPoint3(1d, 0d, 0d),
            HorizontalFieldOfViewDegrees: 110d);
    }

    [Benchmark]
    public StarmapFramePlan PlanFrame()
    {
        return _planner.PlanFrame(_camera);
    }

    private static IReadOnlyList<StarmapSectorNode> BuildSectors(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index =>
            {
                var ring = 1 + (index / 120);
                var angle = (index % 120) * (Math.PI * 2d / 120d);
                var radius = ring * 65d;
                return new StarmapSectorNode(
                    SectorId: Guid.NewGuid(),
                    Name: $"S-{index:D5}",
                    Position: new MapPoint3(
                        X: Math.Cos(angle) * radius,
                        Y: Math.Sin(angle) * radius,
                        Z: (index % 9) * 14d),
                    IsHub: index % 41 == 0);
            })
            .ToArray();
    }

    private static IReadOnlyList<StarmapRouteEdge> BuildRoutes(IReadOnlyList<StarmapSectorNode> sectors)
    {
        var routes = new List<StarmapRouteEdge>(sectors.Count * 3);
        for (var index = 0; index < sectors.Count; index++)
        {
            var from = sectors[index];
            var next = sectors[(index + 1) % sectors.Count];
            var jump = sectors[(index + 17) % sectors.Count];
            routes.Add(new StarmapRouteEdge(Guid.NewGuid(), from.SectorId, next.SectorId, IsHighRisk: index % 7 == 0));
            routes.Add(new StarmapRouteEdge(Guid.NewGuid(), from.SectorId, jump.SectorId, IsHighRisk: index % 11 == 0));
        }

        return routes;
    }
}
