using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class GlobalMetricsServiceTests
{
    [Fact]
    public async Task GetGlobalSummaryAsync_ComputesTelemetrySnapshot()
    {
        await using var dbContext = CreateDbContext();
        var nowUtc = DateTime.UtcNow;
        await SeedPlayersAsync(dbContext, nowUtc);
        await SeedCombatLogsAsync(dbContext, nowUtc);
        await SeedTradeTransactionsAsync(dbContext, nowUtc);

        var service = new GlobalMetricsService(dbContext);
        var summary = await service.GetGlobalSummaryAsync();

        Assert.Equal(3, summary.TotalUsers);
        Assert.Equal(2, summary.ActivePlayers24h);
        Assert.Equal(0.25m, summary.AvgBattlesPerHour);
        Assert.Equal(50m, summary.EconomicStabilityIndex);
        Assert.Equal("vex", summary.TopReputationPlayer.Username);
        Assert.Equal(90m, summary.TopReputationPlayer.Score);
        Assert.Equal("nova", summary.TopFinancialPlayer.Username);
        Assert.Equal(5000m, summary.TopFinancialPlayer.Score);
    }

    private static async Task SeedPlayersAsync(GalacticTraderDbContext dbContext, DateTime nowUtc)
    {
        dbContext.Players.AddRange(
            new Player
            {
                Id = Guid.NewGuid(),
                Username = "nova",
                Email = "nova@gt.local",
                KeycloakUserId = Guid.NewGuid(),
                NetWorth = 5000m,
                LiquidCredits = 2000m,
                ReputationScore = 80,
                AlignmentLevel = 0,
                FleetStrengthRating = 0,
                ProtectionStatus = "guarded",
                CreatedAt = nowUtc.AddDays(-10),
                LastActiveAt = nowUtc.AddHours(-1),
                IsActive = true
            },
            new Player
            {
                Id = Guid.NewGuid(),
                Username = "vex",
                Email = "vex@gt.local",
                KeycloakUserId = Guid.NewGuid(),
                NetWorth = 3200m,
                LiquidCredits = 1200m,
                ReputationScore = 90,
                AlignmentLevel = 0,
                FleetStrengthRating = 0,
                ProtectionStatus = "guarded",
                CreatedAt = nowUtc.AddDays(-5),
                LastActiveAt = nowUtc.AddHours(-5),
                IsActive = true
            },
            new Player
            {
                Id = Guid.NewGuid(),
                Username = "drift",
                Email = "drift@gt.local",
                KeycloakUserId = Guid.NewGuid(),
                NetWorth = 1100m,
                LiquidCredits = 300m,
                ReputationScore = 40,
                AlignmentLevel = 0,
                FleetStrengthRating = 0,
                ProtectionStatus = "fragile",
                CreatedAt = nowUtc.AddDays(-2),
                LastActiveAt = nowUtc.AddDays(-3),
                IsActive = false
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCombatLogsAsync(GalacticTraderDbContext dbContext, DateTime nowUtc)
    {
        var playerId = dbContext.Players.Select(static player => player.Id).First();
        var sector = new Sector
        {
            Id = Guid.NewGuid(),
            Name = "Orion",
            X = 0f,
            Y = 0f,
            Z = 0f,
            SecurityLevel = 60,
            HazardRating = 20,
            ResourceModifier = 1.1f,
            ControlledByFactionId = null,
            EconomicIndex = 75,
            SensorInterferenceLevel = 0.1f
        };
        var ship = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Name = "Spear",
            ShipClass = "Frigate",
            HullIntegrity = 100,
            MaxHullIntegrity = 100,
            ShieldCapacity = 80,
            MaxShieldCapacity = 80,
            ReactorOutput = 100,
            CargoCapacity = 40,
            SensorRange = 25,
            SignatureProfile = 20,
            CrewSlots = 6,
            Hardpoints = 3,
            CurrentValue = 900m,
            CurrentSectorId = sector.Id,
            TargetSectorId = sector.Id,
            PurchasedAt = nowUtc.AddDays(-2),
            PurchasePrice = 1000m
        };

        dbContext.Sectors.Add(sector);
        dbContext.Ships.Add(ship);
        dbContext.CombatLogs.AddRange(
            Enumerable.Range(0, 6).Select(index => new CombatLog
            {
                Id = Guid.NewGuid(),
                AttackerId = playerId,
                DefenderId = null,
                LocationSectorId = sector.Id,
                AttackerShipId = ship.Id,
                DefenderShipId = null,
                AttackerInitialRating = 50,
                DefenderInitialRating = 35,
                BattleOutcome = "victory",
                AttackerDamageDealt = 40,
                DefenderDamageDealt = 15,
                AttackerHullDamage = 3,
                DefenderHullDamage = 25,
                AttackerReward = 10m,
                DefenderCompensation = 0m,
                InsurancePayout = 0m,
                AttackerReputationChange = 1,
                DefenderReputationChange = 0,
                BattleStartedAt = nowUtc.AddHours(-(index + 2)),
                BattleEndedAt = nowUtc.AddHours(-(index + 1)),
                DurationSeconds = 45,
                TotalTicks = 12
            }));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedTradeTransactionsAsync(GalacticTraderDbContext dbContext, DateTime nowUtc)
    {
        var playerId = dbContext.Players.Select(static player => player.Id).First();
        var commodity = new Commodity
        {
            Id = Guid.NewGuid(),
            Name = "Ore",
            Category = "Raw",
            Description = "Industrial ore",
            BasePrice = 100f,
            Volume = 1f,
            LegalityFactor = 1f,
            Rarity = 20f
        };
        var sector = dbContext.Sectors.First();
        var market = new Market
        {
            Id = Guid.NewGuid(),
            SectorId = sector.Id,
            LastUpdated = nowUtc
        };

        dbContext.Commodities.Add(commodity);
        dbContext.Markets.Add(market);
        dbContext.TradeTransactions.AddRange(
            new TradeTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                SellerId = Guid.NewGuid(),
                CommodityId = commodity.Id,
                FromMarketId = market.Id,
                ToMarketId = market.Id,
                Quantity = 1,
                PricePerUnit = 100m,
                TotalPrice = 100m,
                Tariff = 0m,
                TaxAmount = 0m,
                TransactionFee = 0m,
                InsuranceCost = 0m,
                NetProfit = 0m,
                Status = "completed",
                UsedSmugglingRoute = false,
                CreatedAt = nowUtc.AddHours(-4),
                CompletedAt = nowUtc.AddHours(-3)
            },
            new TradeTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                SellerId = Guid.NewGuid(),
                CommodityId = commodity.Id,
                FromMarketId = market.Id,
                ToMarketId = market.Id,
                Quantity = 1,
                PricePerUnit = 300m,
                TotalPrice = 300m,
                Tariff = 0m,
                TaxAmount = 0m,
                TransactionFee = 0m,
                InsuranceCost = 0m,
                NetProfit = 0m,
                Status = "completed",
                UsedSmugglingRoute = false,
                CreatedAt = nowUtc.AddHours(-2),
                CompletedAt = nowUtc.AddHours(-1)
            });

        await dbContext.SaveChangesAsync();
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"global-metrics-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
