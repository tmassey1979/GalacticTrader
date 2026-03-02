using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Navigation;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class GraphValidationServiceTests
{
    [Fact]
    public async Task EnsureSectorCanBeCreatedAsync_Throws_WhenNameAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Sectors.Add(new Sector { Id = Guid.NewGuid(), Name = "Alpha", X = 0, Y = 0, Z = 0 });
        await dbContext.SaveChangesAsync();

        var validator = CreateGraphValidationService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.EnsureSectorCanBeCreatedAsync("Alpha"));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnsureRouteCanBeCreatedAsync_Throws_WhenRouteIsSelfLoop()
    {
        await using var dbContext = CreateDbContext();
        var sectorId = Guid.NewGuid();
        dbContext.Sectors.Add(new Sector { Id = sectorId, Name = "Loop", X = 1, Y = 1, Z = 1 });
        await dbContext.SaveChangesAsync();

        var validator = CreateGraphValidationService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.EnsureRouteCanBeCreatedAsync(sectorId, sectorId));

        Assert.Contains("same sector", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateGraphAsync_ReturnsErrorsAndWarnings_ForDuplicateAndIsolatedNodes()
    {
        await using var dbContext = CreateDbContext();

        var alpha = new Sector { Id = Guid.NewGuid(), Name = "Alpha", X = 0, Y = 0, Z = 0 };
        var beta = new Sector { Id = Guid.NewGuid(), Name = "Beta", X = 5, Y = 0, Z = 0 };
        var gamma = new Sector { Id = Guid.NewGuid(), Name = "Gamma", X = 9, Y = 0, Z = 0 };

        dbContext.Sectors.AddRange(alpha, beta, gamma);
        dbContext.Routes.Add(new Route
        {
            Id = Guid.NewGuid(),
            FromSectorId = alpha.Id,
            ToSectorId = beta.Id,
            LegalStatus = "Legal",
            WarpGateType = "Standard"
        });
        dbContext.Routes.Add(new Route
        {
            Id = Guid.NewGuid(),
            FromSectorId = alpha.Id,
            ToSectorId = beta.Id,
            LegalStatus = "Legal",
            WarpGateType = "Standard"
        });

        await dbContext.SaveChangesAsync();

        var validator = CreateGraphValidationService(dbContext);
        var report = await validator.ValidateGraphAsync();

        Assert.False(report.IsValid);
        Assert.NotEmpty(report.Errors);
        Assert.Contains(report.Warnings, warning => warning.Contains("Gamma", StringComparison.Ordinal));
    }

    private static GraphValidationService CreateGraphValidationService(GalacticTraderDbContext dbContext)
    {
        return new GraphValidationService(
            new SectorRepository(dbContext),
            new RouteRepository(dbContext));
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase(databaseName: $"graph-validation-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
