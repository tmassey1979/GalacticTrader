using System.Diagnostics;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Combat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GalacticTrader.Tests;

public sealed class CombatServiceTests
{
    [Fact]
    public async Task ProcessTickAsync_IsDeterministic_ForIdenticalInputs()
    {
        var fixedAttackerShipId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var fixedDefenderShipId = Guid.Parse("20000000-0000-0000-0000-000000000002");

        var firstResult = await RunSingleTickAsync(fixedAttackerShipId, fixedDefenderShipId);
        var secondResult = await RunSingleTickAsync(fixedAttackerShipId, fixedDefenderShipId);

        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Equal(firstResult!.AttackerHull, secondResult!.AttackerHull);
        Assert.Equal(firstResult.DefenderHull, secondResult.DefenderHull);
        Assert.Equal(firstResult.Hits.Count, secondResult.Hits.Count);
        Assert.Equal(firstResult.Hits.First().Damage, secondResult.Hits.First().Damage);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task ProcessTickAsync_Performance_Under50MillisecondsAverage()
    {
        await using var dbContext = CreateDbContext();
        var (attackerShipId, defenderShipId) = await SeedCombatDataAsync(dbContext, 1000, 1000);
        var service = new CombatService(dbContext, NullLogger<CombatService>.Instance);

        var combat = await service.StartCombatAsync(new StartCombatRequest
        {
            AttackerShipId = attackerShipId,
            DefenderShipId = defenderShipId,
            MaxTicks = 1000
        });

        var iterations = 200;
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < iterations; index++)
        {
            var tick = await service.ProcessTickAsync(combat.CombatId);
            if (tick is null || tick.State != CombatState.Active)
            {
                break;
            }
        }

        stopwatch.Stop();
        var average = stopwatch.Elapsed.TotalMilliseconds / iterations;
        Assert.True(average < 50d, $"Expected <50ms per tick average, actual: {average:0.00}ms");
    }

    [Fact]
    public async Task EndCombatAsync_PersistsCombatLog_AndInsurancePayout()
    {
        await using var dbContext = CreateDbContext();
        var (attackerShipId, defenderShipId) = await SeedCombatDataAsync(dbContext, 900, 80, defenderInsured: true);
        var service = new CombatService(dbContext, NullLogger<CombatService>.Instance);

        var combat = await service.StartCombatAsync(new StartCombatRequest
        {
            AttackerShipId = attackerShipId,
            DefenderShipId = defenderShipId,
            MaxTicks = 400
        });

        _ = await service.ProcessTicksAsync(combat.CombatId, 400);
        var summary = await service.GetCombatAsync(combat.CombatId);
        if (summary?.State == CombatState.Active)
        {
            await service.EndCombatAsync(combat.CombatId);
        }

        var logs = await service.GetRecentCombatLogsAsync(10);
        Assert.NotEmpty(logs);
        Assert.Contains(logs, log => log.InsurancePayout >= 0m);

        var leaderboard = await dbContext.Leaderboards
            .FirstOrDefaultAsync(entry => entry.LeaderboardType == "combat");
        Assert.NotNull(leaderboard);
    }

    private static async Task<CombatTickResultDto?> RunSingleTickAsync(Guid attackerShipId, Guid defenderShipId)
    {
        await using var dbContext = CreateDbContext();
        await SeedCombatDataAsync(
            dbContext,
            600,
            600,
            attackerShipIdOverride: attackerShipId,
            defenderShipIdOverride: defenderShipId);
        var service = new CombatService(dbContext, NullLogger<CombatService>.Instance);
        var combat = await service.StartCombatAsync(new StartCombatRequest
        {
            AttackerShipId = attackerShipId,
            DefenderShipId = defenderShipId,
            MaxTicks = 100
        });

        return await service.ProcessTickAsync(combat.CombatId);
    }

    private static async Task<(Guid attackerShipId, Guid defenderShipId)> SeedCombatDataAsync(
        GalacticTraderDbContext dbContext,
        int attackerHull,
        int defenderHull,
        bool defenderInsured = false,
        Guid? attackerShipIdOverride = null,
        Guid? defenderShipIdOverride = null)
    {
        var attackerPlayerId = Guid.NewGuid();
        var defenderPlayerId = Guid.NewGuid();
        var sectorId = Guid.NewGuid();

        dbContext.Sectors.Add(new Sector
        {
            Id = sectorId,
            Name = "Arena",
            X = 0,
            Y = 0,
            Z = 0,
            SecurityLevel = 50,
            HazardRating = 20
        });

        dbContext.Players.AddRange(
            new Player
            {
                Id = attackerPlayerId,
                Username = "attacker",
                Email = "attacker@test.local",
                KeycloakUserId = Guid.NewGuid(),
                ReputationScore = 10,
                ProtectionStatus = "None"
            },
            new Player
            {
                Id = defenderPlayerId,
                Username = "defender",
                Email = "defender@test.local",
                KeycloakUserId = Guid.NewGuid(),
                ReputationScore = 10,
                ProtectionStatus = "None"
            });

        var attackerShipId = attackerShipIdOverride ?? Guid.NewGuid();
        var defenderShipId = defenderShipIdOverride ?? Guid.NewGuid();

        var attackerShip = new Ship
        {
            Id = attackerShipId,
            PlayerId = attackerPlayerId,
            Name = "ATK",
            ShipClass = "Frigate",
            HullIntegrity = attackerHull,
            MaxHullIntegrity = attackerHull,
            ShieldCapacity = 200,
            MaxShieldCapacity = 200,
            ReactorOutput = 120,
            CargoCapacity = 100,
            CurrentSectorId = sectorId,
            PurchasePrice = 100_000m,
            CurrentValue = 120_000m,
            InsuranceRate = 0.1m,
            IsActive = true
        };

        var defenderShip = new Ship
        {
            Id = defenderShipId,
            PlayerId = defenderPlayerId,
            Name = "DEF",
            ShipClass = "Destroyer",
            HullIntegrity = defenderHull,
            MaxHullIntegrity = defenderHull,
            ShieldCapacity = 150,
            MaxShieldCapacity = 150,
            ReactorOutput = 100,
            CargoCapacity = 100,
            CurrentSectorId = sectorId,
            PurchasePrice = 90_000m,
            CurrentValue = 100_000m,
            HasInsurance = defenderInsured,
            InsuranceRate = 0.25m,
            IsActive = true
        };

        dbContext.Ships.AddRange(attackerShip, defenderShip);
        dbContext.Crew.AddRange(
            new Crew
            {
                Id = Guid.NewGuid(),
                PlayerId = attackerPlayerId,
                ShipId = attackerShipId,
                Name = "ATK Crew",
                Role = "Gunner",
                CombatSkill = 80,
                EngineeringSkill = 60,
                NavigationSkill = 60,
                Morale = 80,
                Loyalty = 80,
                IsActive = true
            },
            new Crew
            {
                Id = Guid.NewGuid(),
                PlayerId = defenderPlayerId,
                ShipId = defenderShipId,
                Name = "DEF Crew",
                Role = "Gunner",
                CombatSkill = 75,
                EngineeringSkill = 60,
                NavigationSkill = 60,
                Morale = 80,
                Loyalty = 80,
                IsActive = true
            });

        dbContext.ShipModules.AddRange(
            new ShipModule
            {
                Id = Guid.NewGuid(),
                ShipId = attackerShipId,
                ModuleType = "weapon",
                Name = "ATK Cannon",
                Tier = 3,
                HealthPoints = 100,
                MaxHealthPoints = 100
            },
            new ShipModule
            {
                Id = Guid.NewGuid(),
                ShipId = defenderShipId,
                ModuleType = "weapon",
                Name = "DEF Cannon",
                Tier = 2,
                HealthPoints = 100,
                MaxHealthPoints = 100
            });

        await dbContext.SaveChangesAsync();
        return (attackerShipId, defenderShipId);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"combat-tests-{Guid.NewGuid():N}")
            .Options;
        return new GalacticTraderDbContext(options);
    }
}
