using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Fleet;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class FleetServiceTests
{
    [Fact]
    public async Task PurchaseShipAsync_DeductsCreditsAndCreatesShip()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 900_000m);
        var service = new FleetService(dbContext);

        var ship = await service.PurchaseShipAsync(new PurchaseShipRequest
        {
            PlayerId = player.Id,
            TemplateKey = "escort",
            Name = "Guardian"
        });

        var updatedPlayer = await dbContext.Players.FirstAsync(existing => existing.Id == player.Id);
        Assert.NotNull(ship);
        Assert.Equal("Guardian", ship!.Name);
        Assert.Equal("Escort", ship.ShipClass);
        Assert.True(updatedPlayer.LiquidCredits < 900_000m);
    }

    [Fact]
    public async Task InstallModuleAsync_EnforcesHardpointLimit()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 2_000_000m);
        var ship = await SeedShipAsync(dbContext, player.Id, hardpoints: 2);
        var service = new FleetService(dbContext);

        await service.InstallModuleAsync(new InstallShipModuleRequest { ShipId = ship.Id, ModuleType = "weapon", Name = "Railgun-I", Tier = 1 });
        await service.InstallModuleAsync(new InstallShipModuleRequest { ShipId = ship.Id, ModuleType = "weapon", Name = "Railgun-II", Tier = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.InstallModuleAsync(new InstallShipModuleRequest
            {
                ShipId = ship.Id,
                ModuleType = "weapon",
                Name = "Railgun-III",
                Tier = 1
            }));
    }

    [Fact]
    public async Task HireAndProgressCrew_UpdatesExperience()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 300_000m);
        var ship = await SeedShipAsync(dbContext, player.Id, crewSlots: 4);
        var service = new FleetService(dbContext);

        var crew = await service.HireCrewAsync(new HireCrewRequest
        {
            PlayerId = player.Id,
            ShipId = ship.Id,
            Name = "Mara",
            Role = "Engineer",
            Salary = 2400m
        });

        var progressed = await service.ProgressCrewAsync(crew!.Id, new CrewProgressRequest
        {
            ExperienceGained = 2_500,
            MissionOutcomeScore = 18
        });

        Assert.NotNull(progressed);
        Assert.True(progressed!.ExperienceLevel >= 3);
        Assert.True(progressed.EngineeringSkill >= crew.EngineeringSkill);
    }

    [Fact]
    public async Task SimulateConvoyAsync_ReturnsProtectedValue()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 4_000_000m);
        await SeedShipAsync(dbContext, player.Id, shipClass: "Escort", hardpoints: 4, sensorRange: 120);
        await SeedShipAsync(dbContext, player.Id, shipClass: "Hauler", hardpoints: 2, sensorRange: 90);
        var service = new FleetService(dbContext);

        var result = await service.SimulateConvoyAsync(new ConvoySimulationRequest
        {
            PlayerId = player.Id,
            Formation = FleetFormation.Defensive,
            ConvoyValue = 500_000m
        });

        Assert.NotNull(result);
        Assert.True(result!.ProjectedProtectedValue > 0m);
        Assert.InRange(result.ExpectedLossPercent, 2, 70);
    }

    [Fact]
    public async Task GetShipTemplatesAsync_ReturnsKnownTemplates()
    {
        await using var dbContext = CreateDbContext();
        var service = new FleetService(dbContext);

        var templates = await service.GetShipTemplatesAsync();

        Assert.Contains(templates, template => template.Key.Equals("scout", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, template => template.Key.Equals("escort", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPlayerShipsAndGetShipAsync_ReturnPurchasedShip()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 1_000_000m);
        var service = new FleetService(dbContext);

        var purchased = await service.PurchaseShipAsync(new PurchaseShipRequest
        {
            PlayerId = player.Id,
            TemplateKey = "scout",
            Name = "Scout One"
        });

        Assert.NotNull(purchased);

        var ships = await service.GetPlayerShipsAsync(player.Id);
        var fetched = await service.GetShipAsync(purchased!.Id);

        Assert.Single(ships);
        Assert.NotNull(fetched);
        Assert.Equal(purchased.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetPlayerShipsAsync_MapsInsuranceAndAssignedRoute()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 500_000m);
        var alpha = await SeedSectorAsync(dbContext, "Alpha");
        var beta = await SeedSectorAsync(dbContext, "Beta");
        dbContext.Ships.Add(new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Name = "Runner",
            ShipClass = "Hauler",
            HullIntegrity = 240,
            MaxHullIntegrity = 240,
            ShieldCapacity = 180,
            MaxShieldCapacity = 180,
            ReactorOutput = 100,
            CargoCapacity = 480,
            CargoUsed = 0,
            SensorRange = 80,
            SignatureProfile = 40,
            CrewSlots = 8,
            Hardpoints = 2,
            HasInsurance = false,
            InsuranceRate = 0m,
            IsActive = true,
            IsInCombat = false,
            StatusId = 1,
            CurrentSectorId = alpha.Id,
            TargetSectorId = beta.Id,
            PurchasePrice = 120_000m,
            CurrentValue = 110_000m,
            PurchasedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var service = new FleetService(dbContext);

        var ships = await service.GetPlayerShipsAsync(player.Id);

        var ship = Assert.Single(ships);
        Assert.False(ship.HasInsurance);
        Assert.Equal(0m, ship.InsuranceRate);
        Assert.Equal("Alpha -> Beta", ship.AssignedRoute);
    }

    [Fact]
    public async Task FireCrewAsync_RemovesCrewRecord()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext, 300_000m);
        var ship = await SeedShipAsync(dbContext, player.Id, crewSlots: 3);
        var service = new FleetService(dbContext);

        var crew = await service.HireCrewAsync(new HireCrewRequest
        {
            PlayerId = player.Id,
            ShipId = ship.Id,
            Name = "Ari",
            Role = "Navigator",
            Salary = 2200m
        });

        Assert.NotNull(crew);

        var fired = await service.FireCrewAsync(crew!.Id);
        var firedAgain = await service.FireCrewAsync(crew.Id);

        Assert.True(fired);
        Assert.False(firedAgain);
    }

    private static async Task<Player> SeedPlayerAsync(GalacticTraderDbContext dbContext, decimal credits)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = $"pilot-{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@gt.local",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = credits,
            LiquidCredits = credits,
            ReputationScore = 0,
            AlignmentLevel = 0,
            FleetStrengthRating = 0,
            ProtectionStatus = "standard",
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Players.Add(player);
        await dbContext.SaveChangesAsync();
        return player;
    }

    private static async Task<Ship> SeedShipAsync(
        GalacticTraderDbContext dbContext,
        Guid playerId,
        string shipClass = "Escort",
        int hardpoints = 3,
        int crewSlots = 8,
        int sensorRange = 100)
    {
        var ship = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Name = $"{shipClass}-{Guid.NewGuid():N}"[..14],
            ShipClass = shipClass,
            HullIntegrity = 350,
            MaxHullIntegrity = 350,
            ShieldCapacity = 220,
            MaxShieldCapacity = 220,
            ReactorOutput = 130,
            CargoCapacity = 200,
            CargoUsed = 0,
            SensorRange = sensorRange,
            SignatureProfile = 50,
            CrewSlots = crewSlots,
            Hardpoints = hardpoints,
            HasInsurance = true,
            InsuranceRate = 0.015m,
            IsActive = true,
            IsInCombat = false,
            StatusId = 0,
            PurchasePrice = 200_000m,
            CurrentValue = 200_000m,
            PurchasedAt = DateTime.UtcNow
        };

        dbContext.Ships.Add(ship);
        await dbContext.SaveChangesAsync();
        return ship;
    }

    private static async Task<Sector> SeedSectorAsync(GalacticTraderDbContext dbContext, string name)
    {
        var sector = new Sector
        {
            Id = Guid.NewGuid(),
            Name = name,
            X = 0f,
            Y = 0f,
            Z = 0f,
            SecurityLevel = 50,
            HazardRating = 20,
            ResourceModifier = 1f,
            EconomicIndex = 55,
            SensorInterferenceLevel = 10f,
            AverageTrafficLevel = 40,
            PiratePresenceProbability = 20
        };

        dbContext.Sectors.Add(sector);
        await dbContext.SaveChangesAsync();
        return sector;
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"fleet-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
