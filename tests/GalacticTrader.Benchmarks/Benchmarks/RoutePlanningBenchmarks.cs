namespace GalacticTrader.Benchmarks;

using BenchmarkDotNet.Attributes;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

[MemoryDiagnoser]
public class RoutePlanningBenchmarks
{
    private GalacticTraderDbContext _dbContext = null!;
    private RoutePlanningService _service = null!;
    private List<Sector> _sectors = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dbContext = CreateDbContext();
        _sectors = SeedDenseGraph(_dbContext, 180);
        _service = new RoutePlanningService(
            new RouteRepository(_dbContext),
            new SectorRepository(_dbContext),
            new InMemoryCacheService(),
            NullLogger<RoutePlanningService>.Instance);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
    }

    [Benchmark]
    public Task<RoutePlanDto?> CalculateDijkstraRoute()
    {
        var from = _sectors[0].Id;
        var to = _sectors[90].Id;
        return _service.CalculateRouteAsync(from, to, TravelMode.Standard, "dijkstra");
    }

    [Benchmark]
    public Task<RoutePlanDto?> CalculateAStarRoute()
    {
        var from = _sectors[15].Id;
        var to = _sectors[140].Id;
        return _service.CalculateRouteAsync(from, to, TravelMode.ArmedEscort, "astar");
    }

    private static List<Sector> SeedDenseGraph(GalacticTraderDbContext dbContext, int sectorCount)
    {
        var sectors = Enumerable.Range(0, sectorCount)
            .Select(index => new Sector
            {
                Id = Guid.NewGuid(),
                Name = $"Sector-{index}",
                X = index * 2,
                Y = index % 11,
                Z = index % 7
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
                    TravelTimeSeconds = 45 + (hop * 8),
                    FuelCost = 0.4f + (hop * 0.15f),
                    BaseRiskScore = 8 + (hop * 3),
                    LegalStatus = hop % 3 == 0 ? "Illegal" : "Legal",
                    WarpGateType = "Standard",
                    TrafficIntensity = 35 + (hop * 6)
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
            .UseInMemoryDatabase($"route-planning-bench-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
