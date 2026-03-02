using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GalacticTrader.Tests;

public sealed class SectorAndRouteServiceTests
{
    [Fact]
    public async Task CreateSectorAsync_PersistsSectorWithDefaults()
    {
        await using var dbContext = CreateDbContext();
        var cache = new InMemoryCacheService();
        var sectorRepository = new SectorRepository(dbContext);
        var routeRepository = new RouteRepository(dbContext);
        var validation = new GraphValidationService(sectorRepository, routeRepository);

        var service = new SectorService(
            sectorRepository,
            routeRepository,
            validation,
            cache,
            NullLogger<SectorService>.Instance);

        var created = await service.CreateSectorAsync("Alpha", 10, 20, 30);

        Assert.Equal("Alpha", created.Name);
        Assert.Equal(50, created.SecurityLevel);
        Assert.Equal(0, created.HazardRating);
        Assert.True(await dbContext.Sectors.AnyAsync(sector => sector.Id == created.Id));
    }

    [Fact]
    public async Task CreateRouteAsync_CalculatesTravelCosts()
    {
        await using var dbContext = CreateDbContext();
        var cache = new InMemoryCacheService();
        var sectorRepository = new SectorRepository(dbContext);
        var routeRepository = new RouteRepository(dbContext);
        var validation = new GraphValidationService(sectorRepository, routeRepository);

        var fromSector = new Sector { Id = Guid.NewGuid(), Name = "From", X = 0, Y = 0, Z = 0 };
        var toSector = new Sector { Id = Guid.NewGuid(), Name = "To", X = 3, Y = 4, Z = 0 };

        dbContext.Sectors.AddRange(fromSector, toSector);
        await dbContext.SaveChangesAsync();

        var routeService = new RouteService(
            routeRepository,
            sectorRepository,
            validation,
            cache,
            NullLogger<RouteService>.Instance);

        var created = await routeService.CreateRouteAsync(fromSector.Id, toSector.Id, "Illegal", "Standard");

        Assert.True(created.TravelTimeSeconds > 0);
        Assert.True(created.FuelCost > 0);
        Assert.Equal(80f, created.BaseRiskScore);
        Assert.Equal(20f, created.VisibilityRating);
    }

    [Fact]
    public async Task GetAdjacentSectorsAsync_ReturnsConnectedSectors()
    {
        await using var dbContext = CreateDbContext();
        var cache = new InMemoryCacheService();
        var sectorRepository = new SectorRepository(dbContext);
        var routeRepository = new RouteRepository(dbContext);
        var validation = new GraphValidationService(sectorRepository, routeRepository);

        var alpha = new Sector { Id = Guid.NewGuid(), Name = "Alpha", X = 0, Y = 0, Z = 0 };
        var beta = new Sector { Id = Guid.NewGuid(), Name = "Beta", X = 1, Y = 0, Z = 0 };
        var gamma = new Sector { Id = Guid.NewGuid(), Name = "Gamma", X = 2, Y = 0, Z = 0 };

        dbContext.Sectors.AddRange(alpha, beta, gamma);
        dbContext.Routes.Add(new Route
        {
            Id = Guid.NewGuid(),
            FromSectorId = alpha.Id,
            ToSectorId = beta.Id,
            LegalStatus = "Legal",
            WarpGateType = "Standard"
        });
        await dbContext.SaveChangesAsync();

        var sectorService = new SectorService(
            sectorRepository,
            routeRepository,
            validation,
            cache,
            NullLogger<SectorService>.Instance);

        var adjacent = (await sectorService.GetAdjacentSectorsAsync(alpha.Id)).ToList();

        Assert.Single(adjacent);
        Assert.Equal(beta.Id, adjacent[0].Id);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase(databaseName: $"nav-services-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
