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
        var services = CreateNavigationServices(dbContext);

        var created = await services.SectorService.CreateSectorAsync("Alpha", 10, 20, 30);

        Assert.Equal("Alpha", created.Name);
        Assert.Equal(50, created.SecurityLevel);
        Assert.Equal(0, created.HazardRating);
        Assert.True(await dbContext.Sectors.AnyAsync(sector => sector.Id == created.Id));
    }

    [Fact]
    public async Task SectorService_ReadUpdateDeleteFlow_CoversCoreQueries()
    {
        await using var dbContext = CreateDbContext();
        var services = CreateNavigationServices(dbContext);

        var alpha = await services.SectorService.CreateSectorAsync("Alpha", 0, 0, 0);
        var beta = await services.SectorService.CreateSectorAsync("Beta", 50, 10, -20);

        var fetchedById = await services.SectorService.GetSectorByIdAsync(alpha.Id);
        var fetchedByName = await services.SectorService.GetSectorByNameAsync("Beta");
        var all = (await services.SectorService.GetAllSectorsAsync()).ToList();

        Assert.NotNull(fetchedById);
        Assert.NotNull(fetchedByName);
        Assert.Equal(2, all.Count);

        var coordsRange = (await services.SectorService.GetSectorsByCoordsRangeAsync(-10, 55, -10, 20, -30, 10)).ToList();
        Assert.Equal(2, coordsRange.Count);

        var factionId = Guid.NewGuid();
        var updated = await services.SectorService.UpdateSectorAsync(beta.Id, securityLevel: 92, hazardRating: 84, factionId: factionId);
        Assert.NotNull(updated);
        Assert.Equal(92, updated!.SecurityLevel);
        Assert.Equal(84, updated.HazardRating);

        var highSecurity = (await services.SectorService.GetHighSecuritySectorsAsync(80)).ToList();
        var highRisk = (await services.SectorService.GetHighRiskSectorsAsync(80)).ToList();
        var byFaction = (await services.SectorService.GetSectorsByFactionAsync(factionId)).ToList();

        Assert.Single(highSecurity);
        Assert.Single(highRisk);
        Assert.Single(byFaction);

        var distribution = await services.SectorService.GetSecurityLevelDistributionAsync();
        Assert.True(distribution["VeryHigh (80-100)"] >= 1);

        var deleted = await services.SectorService.DeleteSectorAsync(alpha.Id);
        var deletedMissing = await services.SectorService.DeleteSectorAsync(Guid.NewGuid());

        Assert.True(deleted);
        Assert.False(deletedMissing);
    }

    [Fact]
    public async Task CreateRouteAsync_CalculatesTravelCosts()
    {
        await using var dbContext = CreateDbContext();
        var services = CreateNavigationServices(dbContext);

        var fromSector = new Sector { Id = Guid.NewGuid(), Name = "From", X = 0, Y = 0, Z = 0 };
        var toSector = new Sector { Id = Guid.NewGuid(), Name = "To", X = 3, Y = 4, Z = 0 };

        dbContext.Sectors.AddRange(fromSector, toSector);
        await dbContext.SaveChangesAsync();

        var created = await services.RouteService.CreateRouteAsync(fromSector.Id, toSector.Id, "Illegal", "Standard");

        Assert.True(created.TravelTimeSeconds > 0);
        Assert.True(created.FuelCost > 0);
        Assert.Equal(80f, created.BaseRiskScore);
        Assert.Equal(20f, created.VisibilityRating);
    }

    [Fact]
    public async Task RouteService_QueryUpdateDistanceAndDeleteFlow_CoversCoreQueries()
    {
        await using var dbContext = CreateDbContext();
        var services = CreateNavigationServices(dbContext);

        var alpha = await services.SectorService.CreateSectorAsync("Alpha", 0, 0, 0);
        var beta = await services.SectorService.CreateSectorAsync("Beta", 4, 0, 0);
        var gamma = await services.SectorService.CreateSectorAsync("Gamma", 8, 0, 0);

        var legalRoute = await services.RouteService.CreateRouteAsync(alpha.Id, beta.Id, "Legal", "Standard");
        var illegalRoute = await services.RouteService.CreateRouteAsync(beta.Id, gamma.Id, "Illegal", "Standard");

        var all = (await services.RouteService.GetAllRoutesAsync()).ToList();
        var byId = await services.RouteService.GetRouteByIdAsync(legalRoute.Id);
        var outbound = (await services.RouteService.GetOutboundRoutesAsync(alpha.Id)).ToList();
        var inbound = (await services.RouteService.GetInboundRoutesAsync(beta.Id)).ToList();
        var between = (await services.RouteService.GetRoutesBetweenAsync(alpha.Id, beta.Id)).ToList();

        Assert.Equal(2, all.Count);
        Assert.NotNull(byId);
        Assert.Single(outbound);
        Assert.Single(inbound);
        Assert.Single(between);

        var distance = await services.RouteService.GetDistanceBetweenSectorsAsync(alpha.Id, beta.Id);
        Assert.NotNull(distance);
        Assert.True(distance > 0);

        var dangerous = (await services.RouteService.GetDangerousRoutesAsync(70)).ToList();
        var legalOnly = (await services.RouteService.GetLegalRoutesAsync()).ToList();
        Assert.Single(dangerous);
        Assert.Single(legalOnly);

        var updated = await services.RouteService.UpdateRouteAsync(legalRoute.Id, "Illegal", 93f);
        Assert.NotNull(updated);
        Assert.Equal("Illegal", updated!.LegalStatus);
        Assert.Equal(93f, updated.BaseRiskScore);

        var deleted = await services.RouteService.DeleteRouteAsync(illegalRoute.Id);
        var deleteMissing = await services.RouteService.DeleteRouteAsync(Guid.NewGuid());

        Assert.True(deleted);
        Assert.False(deleteMissing);
    }

    [Fact]
    public async Task GetAdjacentSectorsAsync_ReturnsConnectedSectors()
    {
        await using var dbContext = CreateDbContext();
        var services = CreateNavigationServices(dbContext);

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

        var adjacent = (await services.SectorService.GetAdjacentSectorsAsync(alpha.Id)).ToList();

        Assert.Single(adjacent);
        Assert.Equal(beta.Id, adjacent[0].Id);
    }

    private static NavigationServices CreateNavigationServices(GalacticTraderDbContext dbContext)
    {
        var cache = new InMemoryCacheService();
        var sectorRepository = new SectorRepository(dbContext);
        var routeRepository = new RouteRepository(dbContext);
        var validation = new GraphValidationService(sectorRepository, routeRepository);

        return new NavigationServices(
            new SectorService(
                sectorRepository,
                routeRepository,
                validation,
                cache,
                NullLogger<SectorService>.Instance),
            new RouteService(
                routeRepository,
                sectorRepository,
                validation,
                cache,
                NullLogger<RouteService>.Instance));
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase(databaseName: $"nav-services-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }

    private sealed record NavigationServices(SectorService SectorService, RouteService RouteService);
}
