using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Leaderboard;
using GalacticTrader.Services.Reputation;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class ReputationAndLeaderboardServiceTests
{
    [Fact]
    public async Task AdjustFactionStandingAsync_CreatesRelationshipWithBenefits()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayersAndFactionAsync(dbContext);
        var service = new ReputationService(dbContext);

        var standing = await service.AdjustFactionStandingAsync(new UpdateFactionStandingRequest
        {
            PlayerId = seeded.PlayerA.Id,
            FactionId = seeded.Faction.Id,
            ReputationDelta = 90,
            Reason = "Major contract completion"
        });

        Assert.NotNull(standing);
        Assert.Equal("Allied", standing!.Tier);
        Assert.True(standing.Benefits.Count > 0);
        Assert.True(standing.HasAccess);
    }

    [Fact]
    public async Task ApplyAlignmentActionAsync_TracksLawfulAndDirtyPaths()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayersAndFactionAsync(dbContext);
        var service = new ReputationService(dbContext);

        var dirty = await service.ApplyAlignmentActionAsync(new AlignmentActionRequest
        {
            PlayerId = seeded.PlayerA.Id,
            ActionType = AlignmentActionType.Piracy,
            Magnitude = 4
        });

        var lawful = await service.ApplyAlignmentActionAsync(new AlignmentActionRequest
        {
            PlayerId = seeded.PlayerB.Id,
            ActionType = AlignmentActionType.InfrastructureInvestment,
            Magnitude = 6
        });

        Assert.NotNull(dirty);
        Assert.NotNull(lawful);
        Assert.Equal("Dirty", dirty!.Path);
        Assert.Equal("Lawful", lawful!.Path);
        Assert.True(dirty.InsuranceCostModifier > lawful.InsuranceCostModifier);
    }

    [Fact]
    public async Task LeaderboardService_RecalculatesAndReturnsPlayerPosition()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayersAndFactionAsync(dbContext);
        var leaderboardService = new LeaderboardService(dbContext);

        var recalculated = await leaderboardService.RecalculateAllAsync();
        var wealthBoard = await leaderboardService.GetLeaderboardAsync("wealth", 10);
        var position = await leaderboardService.GetPlayerPositionAsync(seeded.PlayerA.Id, "wealth");

        Assert.NotEmpty(recalculated);
        Assert.True(wealthBoard.Count >= 2);
        Assert.NotNull(position);
        Assert.True(position!.Rank >= 1);
        Assert.True(position.TotalPlayers >= 2);
    }

    private static async Task<(Player PlayerA, Player PlayerB, Faction Faction)> SeedPlayersAndFactionAsync(GalacticTraderDbContext dbContext)
    {
        var faction = new Faction
        {
            Id = Guid.NewGuid(),
            Name = "Civitas Union",
            Description = "Lawful trade bloc",
            AlignmentBias = 60,
            InfluenceScore = 75f,
            WealthScore = 70f,
            PowerScore = 68f,
            ReputationMultiplier = 1.1,
            ReputationDecayPerDay = 1,
            ControlledSectors = 4,
            TreasuryBalance = 4_000_000m,
            TradeGoodModifier = 0.95m,
            TaxRate = 0.08m
        };

        var playerA = new Player
        {
            Id = Guid.NewGuid(),
            Username = $"alpha-{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@gt.local",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = 950_000m,
            LiquidCredits = 300_000m,
            ReputationScore = 35,
            AlignmentLevel = 0,
            FleetStrengthRating = 220,
            ProtectionStatus = "standard",
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            IsActive = true
        };

        var playerB = new Player
        {
            Id = Guid.NewGuid(),
            Username = $"beta-{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@gt.local",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = 640_000m,
            LiquidCredits = 120_000m,
            ReputationScore = 12,
            AlignmentLevel = 0,
            FleetStrengthRating = 180,
            ProtectionStatus = "standard",
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Factions.Add(faction);
        dbContext.Players.AddRange(playerA, playerB);
        await dbContext.SaveChangesAsync();

        return (playerA, playerB, faction);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"reputation-leaderboard-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
