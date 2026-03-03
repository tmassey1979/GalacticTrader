using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Strategic;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class StrategicInsuranceAndIntelligenceTests
{
    [Fact]
    public async Task FileInsuranceClaimAsync_ApprovedClaimCreditsPlayer()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayerShipAndSectorAsync(dbContext);
        var service = new StrategicSystemsService(dbContext);

        var policy = await service.UpsertInsurancePolicyAsync(new UpsertInsurancePolicyRequest
        {
            PlayerId = seeded.Player.Id,
            ShipId = seeded.Ship.Id,
            CoverageRate = 0.8f,
            PremiumPerCycle = 450m,
            RiskTier = "low",
            IsActive = true
        });

        Assert.NotNull(policy);

        var claim = await service.FileInsuranceClaimAsync(new FileInsuranceClaimRequest
        {
            PolicyId = policy!.Id,
            ClaimAmount = 1000m
        });

        Assert.NotNull(claim);
        Assert.Equal("approved", claim!.Status);

        var updatedPlayer = await dbContext.Players.FirstAsync(player => player.Id == seeded.Player.Id);
        Assert.True(updatedPlayer.LiquidCredits > seeded.InitialCredits);
    }

    [Fact]
    public async Task IntelligenceReports_CanBePublishedAndExpired()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayerShipAndSectorAsync(dbContext);
        var service = new StrategicSystemsService(dbContext);

        var network = await service.CreateIntelligenceNetworkAsync(new CreateIntelligenceNetworkRequest
        {
            OwnerPlayerId = seeded.Player.Id,
            Name = "deep scouts",
            AssetCount = 12,
            CoverageScore = 74f
        });
        Assert.NotNull(network);

        var report = await service.PublishIntelligenceReportAsync(new PublishIntelligenceReportRequest
        {
            NetworkId = network!.Id,
            SectorId = seeded.Sector.Id,
            SignalType = "market-anomaly",
            ConfidenceScore = 91f,
            Payload = "High-value convoy movement detected",
            TtlMinutes = 5
        });
        Assert.NotNull(report);

        var activeReports = await service.GetIntelligenceReportsAsync(seeded.Player.Id, null);
        Assert.Single(activeReports);

        var persisted = await dbContext.IntelligenceReports.FirstAsync(entry => entry.Id == report!.Id);
        persisted.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        var expired = await service.ExpireIntelligenceReportsAsync();
        Assert.Equal(1, expired);

        var afterExpiry = await service.GetIntelligenceReportsAsync(seeded.Player.Id, null);
        Assert.Empty(afterExpiry);
    }

    private static async Task<(Player Player, Ship Ship, Sector Sector, decimal InitialCredits)> SeedPlayerShipAndSectorAsync(
        GalacticTraderDbContext dbContext)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = $"strategic-player-{Guid.NewGuid():N}"[..20],
            Email = $"{Guid.NewGuid():N}@gt.local",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = 1_200_000m,
            LiquidCredits = 250_000m,
            ReputationScore = 30,
            AlignmentLevel = 10,
            FleetStrengthRating = 200,
            ProtectionStatus = "standard",
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            IsActive = true
        };

        var sector = new Sector
        {
            Id = Guid.NewGuid(),
            Name = $"Vector Span {Guid.NewGuid():N}"[..20],
            X = 2f,
            Y = 3f,
            Z = 5f,
            SecurityLevel = 60,
            HazardRating = 20,
            ResourceModifier = 1.05f,
            EconomicIndex = 66,
            SensorInterferenceLevel = 14f,
            AverageTrafficLevel = 70,
            PiratePresenceProbability = 18
        };

        var ship = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Name = $"Atlas {Guid.NewGuid():N}"[..16],
            ShipClass = "Freighter",
            HullIntegrity = 120,
            MaxHullIntegrity = 120,
            ShieldCapacity = 80,
            MaxShieldCapacity = 80,
            ReactorOutput = 100,
            CargoCapacity = 200,
            CargoUsed = 0,
            SensorRange = 60,
            SignatureProfile = 40,
            CrewSlots = 10,
            Hardpoints = 4,
            HasInsurance = true,
            InsuranceRate = 0.5m,
            IsActive = true,
            IsInCombat = false,
            CurrentSectorId = sector.Id,
            StatusId = 0,
            PurchasePrice = 750_000m,
            PurchasedAt = DateTime.UtcNow.AddDays(-10),
            CurrentValue = 680_000m
        };

        dbContext.Players.Add(player);
        dbContext.Sectors.Add(sector);
        dbContext.Ships.Add(ship);
        await dbContext.SaveChangesAsync();

        return (player, ship, sector, player.LiquidCredits);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"strategic-insurance-intel-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
