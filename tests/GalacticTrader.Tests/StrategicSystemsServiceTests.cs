using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Strategic;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class StrategicSystemsServiceTests
{
    [Fact]
    public async Task UpsertSectorVolatilityCycleAsync_CreatesAndUpdatesCycle()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedStrategicDataAsync(dbContext);
        var service = new StrategicSystemsService(dbContext);

        var created = await service.UpsertSectorVolatilityCycleAsync(new UpdateSectorVolatilityCycleRequest
        {
            SectorId = seeded.SectorA.Id,
            CurrentPhase = "volatile",
            VolatilityIndex = 67f,
            NextTransitionAt = DateTime.UtcNow.AddHours(2)
        });

        var updated = await service.UpsertSectorVolatilityCycleAsync(new UpdateSectorVolatilityCycleRequest
        {
            SectorId = seeded.SectorA.Id,
            CurrentPhase = "cooldown",
            VolatilityIndex = 42f,
            NextTransitionAt = DateTime.UtcNow.AddHours(5)
        });

        Assert.NotNull(created);
        Assert.NotNull(updated);
        Assert.Equal(created!.Id, updated!.Id);
        Assert.Equal("cooldown", updated.CurrentPhase);
        Assert.Equal(42f, updated.VolatilityIndex);
    }

    [Fact]
    public async Task DeclareCorporateWarAsync_CreatesActiveConflict()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedStrategicDataAsync(dbContext);
        var service = new StrategicSystemsService(dbContext);

        var war = await service.DeclareCorporateWarAsync(new DeclareCorporateWarRequest
        {
            AttackerFactionId = seeded.FactionA.Id,
            DefenderFactionId = seeded.FactionB.Id,
            CasusBelli = "supply lane interdiction",
            Intensity = 74
        });

        var activeWars = await service.GetCorporateWarsAsync(activeOnly: true);

        Assert.NotNull(war);
        Assert.True(war!.IsActive);
        Assert.Single(activeWars);
        Assert.Equal("supply lane interdiction", activeWars[0].CasusBelli);
    }

    [Fact]
    public async Task RecalculateTerritoryDominanceAsync_UsesInfrastructureAndWarMomentum()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedStrategicDataAsync(dbContext);
        var service = new StrategicSystemsService(dbContext);

        await service.UpsertInfrastructureOwnershipAsync(new UpdateInfrastructureOwnershipRequest
        {
            SectorId = seeded.SectorA.Id,
            FactionId = seeded.FactionA.Id,
            InfrastructureType = "warp-gate",
            ControlScore = 85
        });
        await service.UpsertInfrastructureOwnershipAsync(new UpdateInfrastructureOwnershipRequest
        {
            SectorId = seeded.SectorB.Id,
            FactionId = seeded.FactionA.Id,
            InfrastructureType = "shipyard",
            ControlScore = 65
        });
        await service.DeclareCorporateWarAsync(new DeclareCorporateWarRequest
        {
            AttackerFactionId = seeded.FactionA.Id,
            DefenderFactionId = seeded.FactionB.Id,
            CasusBelli = "market seizure",
            Intensity = 60
        });

        var dominance = await service.RecalculateTerritoryDominanceAsync(seeded.FactionA.Id);
        var board = await service.GetTerritoryDominanceAsync();

        Assert.NotNull(dominance);
        Assert.True(dominance!.ControlledSectorCount >= 1);
        Assert.True(dominance.InfrastructureControlScore > 0);
        Assert.True(dominance.DominanceScore > 0);
        Assert.NotEmpty(board);
    }

    private static async Task<(Faction FactionA, Faction FactionB, Sector SectorA, Sector SectorB)> SeedStrategicDataAsync(
        GalacticTraderDbContext dbContext)
    {
        var factionA = new Faction
        {
            Id = Guid.NewGuid(),
            Name = $"Orion Syndicate {Guid.NewGuid():N}"[..20],
            Description = "Aggressive expansion bloc",
            AlignmentBias = -20,
            InfluenceScore = 77f,
            WealthScore = 80f,
            PowerScore = 84f,
            ReputationMultiplier = 1.05,
            ReputationDecayPerDay = 1,
            ControlledSectors = 0,
            TreasuryBalance = 7_500_000m,
            TradeGoodModifier = 1.1m,
            TaxRate = 0.12m
        };

        var factionB = new Faction
        {
            Id = Guid.NewGuid(),
            Name = $"Helios Compact {Guid.NewGuid():N}"[..20],
            Description = "Defensive trade alliance",
            AlignmentBias = 35,
            InfluenceScore = 71f,
            WealthScore = 74f,
            PowerScore = 68f,
            ReputationMultiplier = 1.0,
            ReputationDecayPerDay = 1,
            ControlledSectors = 0,
            TreasuryBalance = 6_100_000m,
            TradeGoodModifier = 0.98m,
            TaxRate = 0.09m
        };

        var sectorA = new Sector
        {
            Id = Guid.NewGuid(),
            Name = $"Aster Gate {Guid.NewGuid():N}"[..20],
            X = 10f,
            Y = 5f,
            Z = 2f,
            SecurityLevel = 62,
            HazardRating = 28,
            ResourceModifier = 1.12f,
            EconomicIndex = 73,
            SensorInterferenceLevel = 19f,
            ControlledByFactionId = factionA.Id,
            AverageTrafficLevel = 81,
            PiratePresenceProbability = 17
        };

        var sectorB = new Sector
        {
            Id = Guid.NewGuid(),
            Name = $"Nadir Reach {Guid.NewGuid():N}"[..20],
            X = -8f,
            Y = 11f,
            Z = -3f,
            SecurityLevel = 54,
            HazardRating = 35,
            ResourceModifier = 0.96f,
            EconomicIndex = 58,
            SensorInterferenceLevel = 22f,
            ControlledByFactionId = factionB.Id,
            AverageTrafficLevel = 63,
            PiratePresenceProbability = 29
        };

        dbContext.Factions.AddRange(factionA, factionB);
        dbContext.Sectors.AddRange(sectorA, sectorB);
        await dbContext.SaveChangesAsync();

        return (factionA, factionB, sectorA, sectorB);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"strategic-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
