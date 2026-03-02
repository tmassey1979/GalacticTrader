using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Communication;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Tests;

public sealed class CommunicationServiceTests
{
    [Fact]
    public async Task SendMessageAsync_PersistsAndReturnsRecentMessages()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext);
        var service = new CommunicationService(dbContext);

        await service.SubscribeAsync(new SubscribeChannelRequest
        {
            PlayerId = player.Id,
            ChannelType = ChannelType.Global,
            ChannelKey = "global"
        });

        var sent = await service.SendMessageAsync(new SendChannelMessageRequest
        {
            PlayerId = player.Id,
            ChannelType = ChannelType.Global,
            ChannelKey = "global",
            Content = "market pulse strong"
        });

        var messages = await service.GetRecentMessagesAsync(ChannelType.Global, "global", 10);
        Assert.NotNull(sent);
        Assert.Single(messages);
        Assert.Equal("market pulse strong", messages[0].Content);
    }

    [Fact]
    public async Task SendMessageAsync_EnforcesRateLimiting()
    {
        await using var dbContext = CreateDbContext();
        var player = await SeedPlayerAsync(dbContext);
        var service = new CommunicationService(dbContext);

        await service.SendMessageAsync(new SendChannelMessageRequest
        {
            PlayerId = player.Id,
            ChannelType = ChannelType.Global,
            ChannelKey = "global",
            Content = "first message"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.SendMessageAsync(new SendChannelMessageRequest
            {
                PlayerId = player.Id,
                ChannelType = ChannelType.Global,
                ChannelKey = "global",
                Content = "second message"
            }));
    }

    private static async Task<Player> SeedPlayerAsync(GalacticTraderDbContext dbContext)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = $"chat-{Guid.NewGuid():N}"[..12],
            Email = $"{Guid.NewGuid():N}@gt.local",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = 100_000m,
            LiquidCredits = 100_000m,
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

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"communication-tests-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
