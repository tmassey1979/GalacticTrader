using GalacticTrader.ClientSdk.Routes;
using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Tests;

public sealed class RouteModuleServiceTests
{
    [Fact]
    public async Task LoadStateAsync_BuildsSectorOptionsAndDangerOverlay()
    {
        var alpha = new SectorApiDto { Id = Guid.NewGuid(), Name = "Alpha" };
        var beta = new SectorApiDto { Id = Guid.NewGuid(), Name = "Beta" };
        var gamma = new SectorApiDto { Id = Guid.NewGuid(), Name = "Gamma" };
        var routes = new[]
        {
            new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = alpha.Id, ToSectorId = beta.Id, BaseRiskScore = 35f },
            new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = beta.Id, ToSectorId = gamma.Id, BaseRiskScore = 88f }
        };
        var dangerousRoutes = routes.Where(route => route.BaseRiskScore >= 70f).ToArray();
        var service = CreateService(
            sectors: [gamma, alpha, beta],
            routes: routes,
            dangerousRoutes: dangerousRoutes);

        var state = await service.LoadStateAsync(dangerousRiskThreshold: 70);

        Assert.Equal(["Alpha", "Beta", "Gamma"], state.SectorOptions.Select(static option => option.Name).ToArray());
        Assert.Single(state.DangerousRoutes);
        Assert.Single(state.Overlay.DangerousEdges);
        Assert.True(state.Overlay.DangerousEdges[0].IsHighRisk);
    }

    [Fact]
    public void ParseWaypoints_ResolvesNameAndGuidTokens_AndReportsUnknown()
    {
        var alpha = new RouteSectorOption(Guid.NewGuid(), "Alpha");
        var beta = new RouteSectorOption(Guid.NewGuid(), "Beta");
        var gamma = new RouteSectorOption(Guid.NewGuid(), "Gamma");
        var service = CreateService();
        var waypointInput = $"beta -> {gamma.SectorId}, unknown";

        var parsed = service.ParseWaypoints(waypointInput, [alpha, beta, gamma]);

        Assert.Equal(2, parsed.Waypoints.Count);
        Assert.Contains(parsed.Waypoints, waypoint => waypoint.SectorId == beta.SectorId);
        Assert.Contains(parsed.Waypoints, waypoint => waypoint.SectorId == gamma.SectorId);
        Assert.Equal(["unknown"], parsed.UnmatchedTokens);
    }

    [Fact]
    public async Task PlanRouteAsync_ChainsWaypointSegments_AndProjectsRiskAndOverlay()
    {
        var alpha = new SectorApiDto { Id = Guid.NewGuid(), Name = "Alpha" };
        var beta = new SectorApiDto { Id = Guid.NewGuid(), Name = "Beta" };
        var gamma = new SectorApiDto { Id = Guid.NewGuid(), Name = "Gamma" };
        var delta = new SectorApiDto { Id = Guid.NewGuid(), Name = "Delta" };
        var routes = new[]
        {
            new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = alpha.Id, ToSectorId = beta.Id, BaseRiskScore = 42f },
            new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = beta.Id, ToSectorId = gamma.Id, BaseRiskScore = 76f },
            new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = gamma.Id, ToSectorId = delta.Id, BaseRiskScore = 61f }
        };
        var plans = new Dictionary<(Guid From, Guid To), RoutePlanApiDto>
        {
            [(alpha.Id, beta.Id)] = CreatePlan(alpha.Id, beta.Id, routes[0].Id, alpha.Name, beta.Name, 42f, 35, 14d, 2.2d),
            [(beta.Id, gamma.Id)] = CreatePlan(beta.Id, gamma.Id, routes[1].Id, beta.Name, gamma.Name, 76f, 40, 18d, 2.8d),
            [(gamma.Id, delta.Id)] = CreatePlan(gamma.Id, delta.Id, routes[2].Id, gamma.Name, delta.Name, 61f, 30, 11d, 1.9d)
        };

        var service = CreateService(
            sectors: [alpha, beta, gamma, delta],
            routes: routes,
            dangerousRoutes: routes.Where(route => route.BaseRiskScore >= 70f).ToArray(),
            routePlanResolver: (from, to) => plans.TryGetValue((from, to), out var plan) ? plan : null);

        var state = await service.LoadStateAsync();
        var result = await service.PlanRouteAsync(
            state,
            fromSectorToken: "Alpha",
            toSectorToken: "Delta",
            waypointInput: "Beta -> Gamma",
            travelModePreset: RouteTravelModePreset.ArmedEscort,
            algorithm: "astar",
            highRiskThreshold: 70);

        Assert.Equal(3, result.Plan.Hops.Count);
        Assert.Equal([alpha.Id, beta.Id, gamma.Id, delta.Id], result.Plan.SectorPath.ToArray());
        Assert.Equal(2, result.WaypointParse.Waypoints.Count);
        Assert.Equal("astar", result.Plan.Algorithm);
        Assert.Equal(3, result.Overlay.PlannedEdges.Count);
        Assert.Single(result.Overlay.DangerousEdges);
        Assert.True(result.RiskSimulation.ProjectedRiskScore is >= 0d and <= 100d);
        Assert.True(result.RiskSimulation.ProtectionCostEstimateCredits > 0m);
    }

    [Fact]
    public async Task LoadOptimizationAsync_SelectsRecommendedPlan_AndBuildsSuggestedOverlay()
    {
        var alpha = new SectorApiDto { Id = Guid.NewGuid(), Name = "Alpha" };
        var beta = new SectorApiDto { Id = Guid.NewGuid(), Name = "Beta" };
        var delta = new SectorApiDto { Id = Guid.NewGuid(), Name = "Delta" };
        var routeA = new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = alpha.Id, ToSectorId = beta.Id, BaseRiskScore = 82f };
        var routeB = new RouteApiDto { Id = Guid.NewGuid(), FromSectorId = beta.Id, ToSectorId = delta.Id, BaseRiskScore = 28f };
        var routes = new[] { routeA, routeB };
        var fastest = CreateCompositePlan(
            fromSectorId: alpha.Id,
            toSectorId: delta.Id,
            hops:
            [
                new RouteHopApiDto { RouteId = routeA.Id, FromSectorId = alpha.Id, ToSectorId = beta.Id, FromSectorName = "Alpha", ToSectorName = "Beta", BaseRiskScore = 82f, BaseTravelTimeSeconds = 25, BaseFuelCost = 14f },
                new RouteHopApiDto { RouteId = routeB.Id, FromSectorId = beta.Id, ToSectorId = delta.Id, FromSectorName = "Beta", ToSectorName = "Delta", BaseRiskScore = 28f, BaseTravelTimeSeconds = 20, BaseFuelCost = 11f }
            ],
            totalTravelSeconds: 45,
            totalRisk: 110d,
            totalFuelCost: 25d,
            totalCost: 21d);
        var balanced = CreateCompositePlan(
            fromSectorId: alpha.Id,
            toSectorId: delta.Id,
            hops:
            [
                new RouteHopApiDto { RouteId = routeA.Id, FromSectorId = alpha.Id, ToSectorId = beta.Id, FromSectorName = "Alpha", ToSectorName = "Beta", BaseRiskScore = 82f, BaseTravelTimeSeconds = 35, BaseFuelCost = 10f },
                new RouteHopApiDto { RouteId = routeB.Id, FromSectorId = beta.Id, ToSectorId = delta.Id, FromSectorName = "Beta", ToSectorName = "Delta", BaseRiskScore = 28f, BaseTravelTimeSeconds = 40, BaseFuelCost = 8f }
            ],
            totalTravelSeconds: 75,
            totalRisk: 35d,
            totalFuelCost: 18d,
            totalCost: 16d);

        var service = CreateService(
            sectors: [alpha, beta, delta],
            routes: routes,
            dangerousRoutes: [routeA],
            routeOptimization: new RouteOptimizationApiDto
            {
                Fastest = fastest,
                Balanced = balanced
            });

        var state = await service.LoadStateAsync();
        var result = await service.LoadOptimizationAsync(state, "Alpha", "Delta", highRiskThreshold: 70);

        Assert.Equal(RouteOptimizationProfile.Balanced, result.RecommendedProfile);
        Assert.Equal(2, result.Overlay.SuggestedEdges.Count);
        Assert.Single(result.Overlay.DangerousEdges);
        Assert.NotNull(result.RecommendedRiskSimulation);
    }

    private static RouteModuleService CreateService(
        IReadOnlyList<SectorApiDto>? sectors = null,
        IReadOnlyList<RouteApiDto>? routes = null,
        IReadOnlyList<RouteApiDto>? dangerousRoutes = null,
        Func<Guid, Guid, RoutePlanApiDto?>? routePlanResolver = null,
        RouteOptimizationApiDto? routeOptimization = null)
    {
        sectors ??= [];
        routes ??= [];
        dangerousRoutes ??= [];
        routeOptimization ??= new RouteOptimizationApiDto();

        var dataSource = new RouteDataSource
        {
            LoadSectorsAsync = _ => Task.FromResult(sectors),
            LoadRoutesAsync = _ => Task.FromResult(routes),
            LoadDangerousRoutesAsync = (_, _) => Task.FromResult(dangerousRoutes),
            LoadRoutePlanAsync = (fromSectorId, toSectorId, _, _, _) => Task.FromResult(routePlanResolver?.Invoke(fromSectorId, toSectorId)),
            LoadRouteOptimizationAsync = (_, _, _) => Task.FromResult(routeOptimization)
        };

        return new RouteModuleService(dataSource);
    }

    private static RoutePlanApiDto CreatePlan(
        Guid fromSectorId,
        Guid toSectorId,
        Guid routeId,
        string fromName,
        string toName,
        float baseRisk,
        int travelSeconds,
        double totalCost,
        double fuelCost)
    {
        return CreateCompositePlan(
            fromSectorId,
            toSectorId,
            [
                new RouteHopApiDto
                {
                    RouteId = routeId,
                    FromSectorId = fromSectorId,
                    ToSectorId = toSectorId,
                    FromSectorName = fromName,
                    ToSectorName = toName,
                    BaseRiskScore = baseRisk,
                    BaseTravelTimeSeconds = travelSeconds,
                    BaseFuelCost = (float)fuelCost
                }
            ],
            travelSeconds,
            baseRisk,
            fuelCost,
            totalCost);
    }

    private static RoutePlanApiDto CreateCompositePlan(
        Guid fromSectorId,
        Guid toSectorId,
        IReadOnlyList<RouteHopApiDto> hops,
        int totalTravelSeconds,
        double totalRisk,
        double totalFuelCost,
        double totalCost)
    {
        var sectorPath = new List<Guid> { fromSectorId };
        foreach (var hop in hops)
        {
            if (sectorPath[^1] != hop.ToSectorId)
            {
                sectorPath.Add(hop.ToSectorId);
            }
        }

        return new RoutePlanApiDto
        {
            FromSectorId = fromSectorId,
            ToSectorId = toSectorId,
            Algorithm = "dijkstra",
            TravelMode = 0,
            TotalCost = totalCost,
            TotalTravelTimeSeconds = totalTravelSeconds,
            TotalFuelCost = totalFuelCost,
            TotalRiskScore = totalRisk,
            SectorPath = sectorPath,
            Hops = hops
        };
    }
}
