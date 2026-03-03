using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class MarketIntelligenceServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ProjectsVolatilityHeatmapTradersAndCorridors()
    {
        await using var dbContext = CreateDbContext();
        var nowUtc = DateTime.UtcNow;
        var players = await SeedPlayersAsync(dbContext, nowUtc);
        var sectors = await SeedSectorsAsync(dbContext);
        var markets = await SeedMarketsAsync(dbContext, sectors);
        var commodity = await SeedCommodityAsync(dbContext);
        await SeedTransactionsAsync(dbContext, nowUtc, players, markets, commodity);

        var service = new MarketIntelligenceService(dbContext);
        var summary = await service.GetSummaryAsync(limit: 5);

        Assert.True(summary.VolatilityIndex > 0m);
        Assert.NotEmpty(summary.RegionalHeatmap);
        Assert.Equal("Orion", summary.RegionalHeatmap[0].SectorName);
        Assert.NotEmpty(summary.TopTraders);
        Assert.Equal("nova", summary.TopTraders[0].Username);
        Assert.NotEmpty(summary.SmugglingCorridors);
        Assert.Equal("Orion", summary.SmugglingCorridors[0].FromSectorName);
        Assert.Equal("Cygnus", summary.SmugglingCorridors[0].ToSectorName);
    }

    private static async Task<IReadOnlyList<Player>> SeedPlayersAsync(GalacticTraderDbContext dbContext, DateTime nowUtc)
    {
        var players = new[]
        {
            new Player
            {
                Id = Guid.NewGuid(),
                Username = "nova",
                Email = "nova@gt.local",
                KeycloakUserId = Guid.NewGuid(),
                NetWorth = 1000m,
                LiquidCredits = 500m,
                ReputationScore = 20,
                AlignmentLevel = 0,
                FleetStrengthRating = 0,
                ProtectionStatus = "none",
                CreatedAt = nowUtc.AddDays(-10),
                LastActiveAt = nowUtc,
                IsActive = true
            },
            new Player
            {
                Id = Guid.NewGuid(),
                Username = "vex",
                Email = "vex@gt.local",
                KeycloakUserId = Guid.NewGuid(),
                NetWorth = 1200m,
                LiquidCredits = 600m,
                ReputationScore = 30,
                AlignmentLevel = 0,
                FleetStrengthRating = 0,
                ProtectionStatus = "none",
                CreatedAt = nowUtc.AddDays(-7),
                LastActiveAt = nowUtc,
                IsActive = true
            }
        };

        dbContext.Players.AddRange(players);
        await dbContext.SaveChangesAsync();
        return players;
    }

    private static async Task<IReadOnlyList<Sector>> SeedSectorsAsync(GalacticTraderDbContext dbContext)
    {
        var sectors = new[]
        {
            new Sector
            {
                Id = Guid.NewGuid(),
                Name = "Orion",
                SecurityLevel = 60,
                HazardRating = 30,
                ResourceModifier = 1.1f,
                EconomicIndex = 70,
                SensorInterferenceLevel = 10f,
                AverageTrafficLevel = 50,
                PiratePresenceProbability = 20
            },
            new Sector
            {
                Id = Guid.NewGuid(),
                Name = "Cygnus",
                SecurityLevel = 55,
                HazardRating = 35,
                ResourceModifier = 1.0f,
                EconomicIndex = 65,
                SensorInterferenceLevel = 12f,
                AverageTrafficLevel = 45,
                PiratePresenceProbability = 28
            }
        };

        dbContext.Sectors.AddRange(sectors);
        await dbContext.SaveChangesAsync();
        return sectors;
    }

    private static async Task<IReadOnlyList<Market>> SeedMarketsAsync(GalacticTraderDbContext dbContext, IReadOnlyList<Sector> sectors)
    {
        var markets = new[]
        {
            new Market { Id = Guid.NewGuid(), SectorId = sectors[0].Id, LastUpdated = DateTime.UtcNow },
            new Market { Id = Guid.NewGuid(), SectorId = sectors[1].Id, LastUpdated = DateTime.UtcNow }
        };

        dbContext.Markets.AddRange(markets);
        await dbContext.SaveChangesAsync();
        return markets;
    }

    private static async Task<Commodity> SeedCommodityAsync(GalacticTraderDbContext dbContext)
    {
        var commodity = new Commodity
        {
            Id = Guid.NewGuid(),
            Name = "Ore",
            Category = "Raw",
            Description = "Ore",
            Volume = 1f,
            BasePrice = 100f,
            LegalityFactor = 1f,
            Rarity = 10f
        };

        dbContext.Commodities.Add(commodity);
        await dbContext.SaveChangesAsync();
        return commodity;
    }

    private static async Task SeedTransactionsAsync(
        GalacticTraderDbContext dbContext,
        DateTime nowUtc,
        IReadOnlyList<Player> players,
        IReadOnlyList<Market> markets,
        Commodity commodity)
    {
        dbContext.TradeTransactions.AddRange(
            new TradeTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = players[0].Id,
                SellerId = Guid.NewGuid(),
                CommodityId = commodity.Id,
                FromMarketId = markets[0].Id,
                ToMarketId = markets[1].Id,
                Quantity = 1,
                PricePerUnit = 500m,
                TotalPrice = 500m,
                Tariff = 0m,
                TaxAmount = 0m,
                TransactionFee = 0m,
                InsuranceCost = 0m,
                NetProfit = 0m,
                Status = "completed",
                UsedSmugglingRoute = true,
                CreatedAt = nowUtc.AddHours(-5),
                CompletedAt = nowUtc.AddHours(-4)
            },
            new TradeTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = players[0].Id,
                SellerId = Guid.NewGuid(),
                CommodityId = commodity.Id,
                FromMarketId = markets[0].Id,
                ToMarketId = markets[0].Id,
                Quantity = 1,
                PricePerUnit = 900m,
                TotalPrice = 900m,
                Tariff = 0m,
                TaxAmount = 0m,
                TransactionFee = 0m,
                InsuranceCost = 0m,
                NetProfit = 0m,
                Status = "completed",
                UsedSmugglingRoute = false,
                CreatedAt = nowUtc.AddHours(-3),
                CompletedAt = nowUtc.AddHours(-2)
            },
            new TradeTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = players[1].Id,
                SellerId = Guid.NewGuid(),
                CommodityId = commodity.Id,
                FromMarketId = markets[1].Id,
                ToMarketId = markets[0].Id,
                Quantity = 1,
                PricePerUnit = 200m,
                TotalPrice = 200m,
                Tariff = 0m,
                TaxAmount = 0m,
                TransactionFee = 0m,
                InsuranceCost = 0m,
                NetProfit = 0m,
                Status = "completed",
                UsedSmugglingRoute = true,
                CreatedAt = nowUtc.AddHours(-2),
                CompletedAt = nowUtc.AddHours(-1)
            });

        await dbContext.SaveChangesAsync();
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"market-intel-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
