using System.Diagnostics;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GalacticTrader.Tests;

public sealed class RoutePlanningServiceTests
{
    [Fact]
    public async Task CalculateRouteAsync_Dijkstra_SelectsLowestCostPath()
    {
        await using var dbContext = CreateDbContext();
        var (alpha, beta, gamma) = SeedSimpleTriangleGraph(dbContext);
        var service = CreatePlanningService(dbContext);

        var plan = await service.CalculateRouteAsync(alpha.Id, gamma.Id, TravelMode.Standard, "dijkstra");

        Assert.NotNull(plan);
        Assert.Equal(3, plan!.SectorPath.Count);
        Assert.Equal(alpha.Id, plan.SectorPath[0]);
        Assert.Equal(beta.Id, plan.SectorPath[1]);
        Assert.Equal(gamma.Id, plan.SectorPath[2]);
    }

    [Fact]
    public async Task CalculateRouteAsync_AStar_MatchesDijkstraPlanCost()
    {
        await using var dbContext = CreateDbContext();
        var (alpha, _, gamma) = SeedSimpleTriangleGraph(dbContext);
        var service = CreatePlanningService(dbContext);

        var dijkstra = await service.CalculateRouteAsync(alpha.Id, gamma.Id, TravelMode.Standard, "dijkstra");
        var aStar = await service.CalculateRouteAsync(alpha.Id, gamma.Id, TravelMode.Standard, "astar");

        Assert.NotNull(dijkstra);
        Assert.NotNull(aStar);
        Assert.Equal(dijkstra!.TotalCost, aStar!.TotalCost);
        Assert.Equal(dijkstra.SectorPath, aStar.SectorPath);
    }

    [Fact]
    public async Task GetOptimizedRoutesAsync_ReturnsModeSpecificRecommendations()
    {
        await using var dbContext = CreateDbContext();
        var (alpha, _, gamma) = SeedSimpleTriangleGraph(dbContext);
        var service = CreatePlanningService(dbContext);

        var optimized = await service.GetOptimizedRoutesAsync(alpha.Id, gamma.Id);

        Assert.NotNull(optimized.Fastest);
        Assert.NotNull(optimized.Cheapest);
        Assert.NotNull(optimized.Safest);
        Assert.NotNull(optimized.Balanced);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task CalculateRouteAsync_Performance_AverageUnder20Milliseconds()
    {
        await using var dbContext = CreateDbContext();
        var sectors = SeedDenseGraph(dbContext, 120);
        var service = CreatePlanningService(dbContext);

        var iterations = 100;
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < iterations; index++)
        {
            var from = sectors[index % sectors.Count].Id;
            var to = sectors[(index + 37) % sectors.Count].Id;

            _ = await service.CalculateRouteAsync(
                from,
                to,
                TravelMode.Standard,
                index % 2 == 0 ? "dijkstra" : "astar");
        }

        stopwatch.Stop();
        var averageMilliseconds = stopwatch.Elapsed.TotalMilliseconds / iterations;

        Assert.True(
            averageMilliseconds < 20d,
            $"Expected <20ms average but measured {averageMilliseconds:0.00}ms.");
    }

    private static RoutePlanningService CreatePlanningService(GalacticTraderDbContext dbContext)
    {
        return new RoutePlanningService(
            new RouteRepository(dbContext),
            new SectorRepository(dbContext),
            new InMemoryCacheService(),
            NullLogger<RoutePlanningService>.Instance);
    }

    private static (Sector Alpha, Sector Beta, Sector Gamma) SeedSimpleTriangleGraph(GalacticTraderDbContext dbContext)
    {
        var alpha = new Sector { Id = Guid.NewGuid(), Name = "Alpha", X = 0, Y = 0, Z = 0 };
        var beta = new Sector { Id = Guid.NewGuid(), Name = "Beta", X = 4, Y = 0, Z = 0 };
        var gamma = new Sector { Id = Guid.NewGuid(), Name = "Gamma", X = 8, Y = 0, Z = 0 };

        dbContext.Sectors.AddRange(alpha, beta, gamma);
        dbContext.Routes.AddRange(
            new Route
            {
                Id = Guid.NewGuid(),
                FromSectorId = alpha.Id,
                ToSectorId = beta.Id,
                TravelTimeSeconds = 100,
                FuelCost = 1.0f,
                BaseRiskScore = 10,
                LegalStatus = "Legal",
                WarpGateType = "Standard"
            },
            new Route
            {
                Id = Guid.NewGuid(),
                FromSectorId = beta.Id,
                ToSectorId = gamma.Id,
                TravelTimeSeconds = 100,
                FuelCost = 1.0f,
                BaseRiskScore = 10,
                LegalStatus = "Legal",
                WarpGateType = "Standard"
            },
            new Route
            {
                Id = Guid.NewGuid(),
                FromSectorId = alpha.Id,
                ToSectorId = gamma.Id,
                TravelTimeSeconds = 300,
                FuelCost = 1.0f,
                BaseRiskScore = 10,
                LegalStatus = "Legal",
                WarpGateType = "Standard"
            });

        dbContext.SaveChanges();
        return (alpha, beta, gamma);
    }

    private static List<Sector> SeedDenseGraph(GalacticTraderDbContext dbContext, int sectorCount)
    {
        var sectors = Enumerable.Range(0, sectorCount)
            .Select(index => new Sector
            {
                Id = Guid.NewGuid(),
                Name = $"Sector-{index}",
                X = index * 3,
                Y = index % 7,
                Z = index % 5
            })
            .ToList();

        dbContext.Sectors.AddRange(sectors);

        var routes = new List<Route>();
        for (var index = 0; index < sectorCount; index++)
        {
            for (var hop = 1; hop <= 4; hop++)
            {
                var nextIndex = (index + hop) % sectorCount;
                routes.Add(new Route
                {
                    Id = Guid.NewGuid(),
                    FromSectorId = sectors[index].Id,
                    ToSectorId = sectors[nextIndex].Id,
                    TravelTimeSeconds = 50 + (hop * 5),
                    FuelCost = 0.5f + (hop * 0.1f),
                    BaseRiskScore = 10 + (hop * 2),
                    LegalStatus = "Legal",
                    WarpGateType = "Standard",
                    TrafficIntensity = 40 + (hop * 5)
                });
            }
        }

        dbContext.Routes.AddRange(routes);
        dbContext.SaveChanges();

        return sectors;
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase(databaseName: $"route-planning-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
