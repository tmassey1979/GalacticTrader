using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Npc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GalacticTrader.Tests;

public sealed class NpcServiceTests
{
    [Fact]
    public async Task CreateAgentAsync_PersistsArchetypeAndDefaults()
    {
        await using var dbContext = CreateDbContext();
        var sectors = await SeedSectorsAndRoutesAsync(dbContext);
        var service = CreateNpcService(dbContext);

        var created = await service.CreateAgentAsync(new CreateNpcRequest
        {
            Name = "Triton",
            Archetype = NpcArchetype.Merchant,
            StartingSectorId = sectors.FromId
        });

        Assert.Equal("Merchant", created.Archetype);
        Assert.Equal("EstablishOperations", created.CurrentGoal);
        Assert.Equal(sectors.FromId, created.CurrentLocationId);
    }

    [Fact]
    public async Task SpawnFleetAsync_CreatesShipsAndCoordinationSummary()
    {
        await using var dbContext = CreateDbContext();
        var sectors = await SeedSectorsAndRoutesAsync(dbContext);
        var service = CreateNpcService(dbContext);
        var agent = await service.CreateAgentAsync(new CreateNpcRequest
        {
            Name = "Corsair",
            Archetype = NpcArchetype.Pirate,
            StartingSectorId = sectors.FromId
        });

        var fleet = await service.SpawnFleetAsync(agent.Id, 4);

        Assert.NotNull(fleet);
        Assert.Equal(4, fleet!.FleetSize);
        Assert.Equal(4, fleet.ActiveShips);
    }

    [Fact]
    public async Task ProcessDecisionTickAsync_UpdatesGoalAndTickCounter()
    {
        await using var dbContext = CreateDbContext();
        var sectors = await SeedSectorsAndRoutesAsync(dbContext);
        await SeedMarketDataAsync(dbContext);
        var service = CreateNpcService(dbContext);
        var agent = await service.CreateAgentAsync(new CreateNpcRequest
        {
            Name = "Broker",
            Archetype = NpcArchetype.ReputableTrader,
            StartingSectorId = sectors.FromId
        });

        var decision = await service.ProcessDecisionTickAsync(agent.Id);

        Assert.NotNull(decision);
        Assert.Equal(1, decision!.DecisionTick);
        Assert.NotEqual(string.Empty, decision.CurrentGoal);
    }

    [Fact]
    public async Task PlanRouteAndMove_AdvancesAgentLocation()
    {
        await using var dbContext = CreateDbContext();
        var sectors = await SeedSectorsAndRoutesAsync(dbContext);
        var service = CreateNpcService(dbContext);
        var agent = await service.CreateAgentAsync(new CreateNpcRequest
        {
            Name = "Nomad",
            Archetype = NpcArchetype.Industrialist,
            StartingSectorId = sectors.FromId
        });
        await service.SpawnFleetAsync(agent.Id, 2);

        var planned = await service.PlanRouteAsync(agent.Id, sectors.ToId);
        var moved = await service.ProcessFleetMovementAsync(agent.Id);
        var updated = await service.GetAgentAsync(agent.Id);

        Assert.True(planned);
        Assert.True(moved);
        Assert.NotNull(updated);
        Assert.NotEqual(sectors.FromId, updated!.CurrentLocationId);
    }

    [Fact]
    public async Task ExecuteNpcTradeAsync_UpdatesNpcWealth()
    {
        await using var dbContext = CreateDbContext();
        var sectors = await SeedSectorsAndRoutesAsync(dbContext);
        await SeedMarketDataAsync(dbContext);
        var service = CreateNpcService(dbContext);
        var agent = await service.CreateAgentAsync(new CreateNpcRequest
        {
            Name = "Trader-X",
            Archetype = NpcArchetype.Merchant,
            StartingSectorId = sectors.FromId
        });

        var margin = await service.ExecuteNpcTradeAsync(agent.Id);
        var updated = await service.GetAgentAsync(agent.Id);

        Assert.True(margin.HasValue);
        Assert.True(margin!.Value >= 0m);
        Assert.NotNull(updated);
        Assert.True(updated!.Wealth >= 0m);
    }

    private static NpcService CreateNpcService(GalacticTraderDbContext dbContext)
    {
        return new NpcService(
            dbContext,
            new RouteRepository(dbContext),
            NullLogger<NpcService>.Instance);
    }

    private static async Task<(Guid FromId, Guid MidId, Guid ToId)> SeedSectorsAndRoutesAsync(GalacticTraderDbContext dbContext)
    {
        var from = new Sector { Id = Guid.NewGuid(), Name = "Alpha", X = 0, Y = 0, Z = 0, EconomicIndex = 40, HazardRating = 20 };
        var mid = new Sector { Id = Guid.NewGuid(), Name = "Beta", X = 5, Y = 0, Z = 0, EconomicIndex = 60, HazardRating = 25 };
        var to = new Sector { Id = Guid.NewGuid(), Name = "Gamma", X = 9, Y = 0, Z = 0, EconomicIndex = 70, HazardRating = 30 };

        dbContext.Sectors.AddRange(from, mid, to);
        dbContext.Routes.AddRange(
            new Route { Id = Guid.NewGuid(), FromSectorId = from.Id, ToSectorId = mid.Id, TravelTimeSeconds = 120, FuelCost = 1.2f, BaseRiskScore = 20, LegalStatus = "Legal", WarpGateType = "Standard" },
            new Route { Id = Guid.NewGuid(), FromSectorId = mid.Id, ToSectorId = to.Id, TravelTimeSeconds = 120, FuelCost = 1.2f, BaseRiskScore = 30, LegalStatus = "Legal", WarpGateType = "Standard" });

        await dbContext.SaveChangesAsync();
        return (from.Id, mid.Id, to.Id);
    }

    private static async Task SeedMarketDataAsync(GalacticTraderDbContext dbContext)
    {
        var sector = await dbContext.Sectors.OrderBy(s => s.Name).FirstAsync();
        var commodity = new Commodity
        {
            Id = Guid.NewGuid(),
            Name = "SynthFiber",
            Category = "industrial",
            Description = "Composite material",
            Volume = 1f,
            BasePrice = 120f,
            LegalityFactor = 1f,
            Rarity = 30f
        };
        var market = new Market
        {
            Id = Guid.NewGuid(),
            SectorId = sector.Id,
            LastUpdated = DateTime.UtcNow
        };
        var listing = new MarketListing
        {
            Id = Guid.NewGuid(),
            MarketId = market.Id,
            CommodityId = commodity.Id,
            BasePrice = 120m,
            CurrentPrice = 130m,
            DemandMultiplier = 1.2f,
            RiskPremium = 0.05f,
            ScarcityModifier = 1.1f,
            AvailableQuantity = 1000,
            MaxQuantity = 2000,
            MinQuantity = 200,
            PriceLastChanged = DateTime.UtcNow
        };

        dbContext.Commodities.Add(commodity);
        dbContext.Markets.Add(market);
        dbContext.MarketListings.Add(listing);
        await dbContext.SaveChangesAsync();
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"npc-tests-{Guid.NewGuid():N}")
            .Options;
        return new GalacticTraderDbContext(options);
    }
}
