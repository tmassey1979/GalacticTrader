using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GalacticTrader.Tests;

public sealed class AutopilotServiceTests
{
    [Fact]
    public async Task StartAutopilotAsync_CreatesTravelingSession()
    {
        await using var dbContext = CreateDbContext();
        var (from, to, _) = SeedAutopilotGraph(dbContext);
        var autopilot = CreateAutopilotService(dbContext);

        var session = await autopilot.StartAutopilotAsync(new StartAutopilotRequest
        {
            ShipId = Guid.NewGuid(),
            FromSectorId = from.Id,
            ToSectorId = to.Id,
            TravelMode = TravelMode.Standard,
            CargoValue = 1000,
            PlayerNotoriety = 5,
            EscortStrength = 10,
            FactionProtection = 10
        });

        Assert.Equal(AutopilotState.Traveling, session.State);
        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.NotEmpty(session.RoutePlan.Hops);
    }

    [Fact]
    public async Task ProcessTickAsync_CompletesSession_WhenEnoughTimePasses()
    {
        await using var dbContext = CreateDbContext();
        var (from, to, _) = SeedAutopilotGraph(dbContext);
        var autopilot = CreateAutopilotService(dbContext);

        var session = await autopilot.StartAutopilotAsync(new StartAutopilotRequest
        {
            ShipId = Guid.NewGuid(),
            FromSectorId = from.Id,
            ToSectorId = to.Id,
            TravelMode = TravelMode.HighBurn
        });

        var tickResult = await autopilot.ProcessTickAsync(session.SessionId, 500);
        Assert.NotNull(tickResult);
        Assert.Equal(AutopilotState.Completed, tickResult!.State);

        var refreshed = await autopilot.GetSessionAsync(session.SessionId);
        Assert.NotNull(refreshed);
        Assert.Equal(AutopilotState.Completed, refreshed!.State);
    }

    [Fact]
    public async Task TransitionTravelModeAsync_RejectsInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var (from, to, _) = SeedAutopilotGraph(dbContext);
        var autopilot = CreateAutopilotService(dbContext);

        var session = await autopilot.StartAutopilotAsync(new StartAutopilotRequest
        {
            ShipId = Guid.NewGuid(),
            FromSectorId = from.Id,
            ToSectorId = to.Id,
            TravelMode = TravelMode.Convoy
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            autopilot.TransitionTravelModeAsync(session.SessionId, TravelMode.GhostRoute, "unsafe switch"));
    }

    private static AutopilotService CreateAutopilotService(GalacticTraderDbContext dbContext)
    {
        var sectorRepository = new SectorRepository(dbContext);
        var routeRepository = new RouteRepository(dbContext);

        var routePlanning = new RoutePlanningService(
            routeRepository,
            sectorRepository,
            new InMemoryCacheService(),
            NullLogger<RoutePlanningService>.Instance);

        return new AutopilotService(
            routePlanning,
            sectorRepository,
            NullLogger<AutopilotService>.Instance);
    }

    private static (Sector From, Sector To, Sector Mid) SeedAutopilotGraph(GalacticTraderDbContext dbContext)
    {
        var from = new Sector { Id = Guid.NewGuid(), Name = "From", X = 0, Y = 0, Z = 0, HazardRating = 10 };
        var mid = new Sector { Id = Guid.NewGuid(), Name = "Mid", X = 3, Y = 0, Z = 0, HazardRating = 20 };
        var to = new Sector { Id = Guid.NewGuid(), Name = "To", X = 6, Y = 0, Z = 0, HazardRating = 15 };

        dbContext.Sectors.AddRange(from, mid, to);
        dbContext.Routes.AddRange(
            new Route
            {
                Id = Guid.NewGuid(),
                FromSectorId = from.Id,
                ToSectorId = mid.Id,
                TravelTimeSeconds = 100,
                FuelCost = 1.0f,
                BaseRiskScore = 40,
                LegalStatus = "Legal",
                WarpGateType = "Standard"
            },
            new Route
            {
                Id = Guid.NewGuid(),
                FromSectorId = mid.Id,
                ToSectorId = to.Id,
                TravelTimeSeconds = 100,
                FuelCost = 1.0f,
                BaseRiskScore = 40,
                LegalStatus = "Legal",
                WarpGateType = "Standard"
            });

        dbContext.SaveChanges();
        return (from, to, mid);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase(databaseName: $"autopilot-{Guid.NewGuid():N}")
            .Options;
        return new GalacticTraderDbContext(options);
    }
}
