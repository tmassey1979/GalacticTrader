using GalacticTrader.ClientSdk.Starmap;

namespace GalacticTrader.Desktop.Tests;

public sealed class StarmapStreamingPlannerTests
{
    [Fact]
    public void ResolveChunk_UsesFloorIndexingForNegativeAndPositiveCoordinates()
    {
        var options = new StarmapChunkingOptions(ChunkSize: 100d, BaseChunkRadius: 1);

        var a = options.ResolveChunk(new MapPoint3(0, 0, 0));
        var b = options.ResolveChunk(new MapPoint3(199.9, -0.1, -100.1));

        Assert.Equal(new StarmapChunkKey(0, 0, 0), a);
        Assert.Equal(new StarmapChunkKey(1, -1, -2), b);
    }

    [Fact]
    public void PlanFrame_AppliesChunkAndRenderBudgets()
    {
        var sectors = BuildGrid(6, spacing: 80d);
        var routes = BuildLinearRoutes(sectors);

        var index = StarmapChunkIndex.Build(
            sectors,
            routes,
            new StarmapChunkingOptions(ChunkSize: 80d, BaseChunkRadius: 2));
        var planner = new StarmapStreamingPlanner(
            index,
            new StarmapRenderBudget(MaxRenderedSectors: 10, MaxRenderedRoutes: 8, MaxActiveChunks: 100),
            StarmapLodBands.StartupDefault);

        var frame = planner.PlanFrame(new StarmapCameraState(
            new MapPoint3(0, 0, 0),
            ViewDistance: 400d,
            Forward: new MapPoint3(1, 0, 0),
            HorizontalFieldOfViewDegrees: 179d));

        Assert.True(frame.ActiveChunks.Count <= 100);
        Assert.Equal(10, frame.Sectors.Count);
        Assert.True(frame.WasSectorBudgetApplied);
        Assert.True(frame.Routes.Count <= 8);
    }

    [Fact]
    public void PlanFrame_AssignsExpectedLodTiersByDistanceBands()
    {
        var sectors = new[]
        {
            new StarmapSectorNode(Guid.NewGuid(), "Near", new MapPoint3(10, 0, 0)),
            new StarmapSectorNode(Guid.NewGuid(), "Mid", new MapPoint3(120, 0, 0)),
            new StarmapSectorNode(Guid.NewGuid(), "Far", new MapPoint3(380, 0, 0))
        };
        var routes = Array.Empty<StarmapRouteEdge>();

        var planner = new StarmapStreamingPlanner(
            StarmapChunkIndex.Build(
                sectors,
                routes,
                new StarmapChunkingOptions(ChunkSize: 100d, BaseChunkRadius: 4)),
            StarmapRenderBudget.Unlimited,
            new StarmapLodBands(NearDistance: 60d, MidDistance: 200d));

        var frame = planner.PlanFrame(new StarmapCameraState(
            new MapPoint3(0, 0, 0),
            ViewDistance: 450d,
            Forward: new MapPoint3(1, 0, 0),
            HorizontalFieldOfViewDegrees: 179d));
        var lodByName = frame.Sectors.ToDictionary(static sector => sector.Name, static sector => sector.LodTier);

        Assert.Equal(StarmapLodTier.Near, lodByName["Near"]);
        Assert.Equal(StarmapLodTier.Mid, lodByName["Mid"]);
        Assert.Equal(StarmapLodTier.Far, lodByName["Far"]);
    }

    [Fact]
    public void PlanFrame_OnlyIncludesRoutesBetweenRenderedSectors()
    {
        var nearA = new StarmapSectorNode(Guid.NewGuid(), "A", new MapPoint3(0, 0, 0));
        var nearB = new StarmapSectorNode(Guid.NewGuid(), "B", new MapPoint3(25, 0, 0));
        var far = new StarmapSectorNode(Guid.NewGuid(), "Far", new MapPoint3(1200, 0, 0));
        var sectors = new[] { nearA, nearB, far };
        var routes = new[]
        {
            new StarmapRouteEdge(Guid.NewGuid(), nearA.SectorId, nearB.SectorId, false),
            new StarmapRouteEdge(Guid.NewGuid(), nearA.SectorId, far.SectorId, false)
        };

        var planner = new StarmapStreamingPlanner(
            StarmapChunkIndex.Build(
                sectors,
                routes,
                new StarmapChunkingOptions(ChunkSize: 100d, BaseChunkRadius: 1)),
            new StarmapRenderBudget(MaxRenderedSectors: 2, MaxRenderedRoutes: 10, MaxActiveChunks: 5),
            StarmapLodBands.StartupDefault);

        var frame = planner.PlanFrame(new StarmapCameraState(new MapPoint3(0, 0, 0), ViewDistance: 100d));

        Assert.Equal(2, frame.Sectors.Count);
        Assert.Single(frame.Routes);
        Assert.Contains(frame.Routes, route => route.FromSectorId == nearA.SectorId && route.ToSectorId == nearB.SectorId);
        Assert.DoesNotContain(frame.Routes, route => route.ToSectorId == far.SectorId || route.FromSectorId == far.SectorId);
    }

    [Fact]
    public void PlanFrame_AppliesFrustumCullingAgainstCameraForward()
    {
        var front = new StarmapSectorNode(Guid.NewGuid(), "Front", new MapPoint3(80, 0, 0));
        var side = new StarmapSectorNode(Guid.NewGuid(), "Side", new MapPoint3(0, 80, 0));
        var rear = new StarmapSectorNode(Guid.NewGuid(), "Rear", new MapPoint3(-80, 0, 0));
        var sectors = new[] { front, side, rear };
        var routes = Array.Empty<StarmapRouteEdge>();

        var planner = new StarmapStreamingPlanner(
            StarmapChunkIndex.Build(sectors, routes, new StarmapChunkingOptions(ChunkSize: 100d, BaseChunkRadius: 2)),
            StarmapRenderBudget.Unlimited,
            StarmapLodBands.StartupDefault);

        var frame = planner.PlanFrame(new StarmapCameraState(
            Position: new MapPoint3(0, 0, 0),
            ViewDistance: 200d,
            Forward: new MapPoint3(1, 0, 0),
            HorizontalFieldOfViewDegrees: 90d));

        Assert.Contains(frame.Sectors, sector => sector.Name == "Front");
        Assert.DoesNotContain(frame.Sectors, sector => sector.Name == "Side");
        Assert.DoesNotContain(frame.Sectors, sector => sector.Name == "Rear");
    }

    private static IReadOnlyList<StarmapSectorNode> BuildGrid(int sideLength, double spacing)
    {
        var sectors = new List<StarmapSectorNode>();
        var index = 0;
        for (var x = 0; x < sideLength; x++)
        {
            for (var y = 0; y < sideLength; y++)
            {
                sectors.Add(new StarmapSectorNode(
                    Guid.NewGuid(),
                    $"S-{index++:D2}",
                    new MapPoint3(x * spacing, y * spacing, 0d),
                    IsHub: (x + y) % 5 == 0));
            }
        }

        return sectors;
    }

    private static IReadOnlyList<StarmapRouteEdge> BuildLinearRoutes(IReadOnlyList<StarmapSectorNode> sectors)
    {
        var routes = new List<StarmapRouteEdge>();
        for (var i = 0; i < sectors.Count - 1; i++)
        {
            routes.Add(new StarmapRouteEdge(
                Guid.NewGuid(),
                sectors[i].SectorId,
                sectors[i + 1].SectorId,
                IsHighRisk: i % 4 == 0));
        }

        return routes;
    }
}
