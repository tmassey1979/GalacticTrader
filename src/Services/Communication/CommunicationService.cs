namespace GalacticTrader.Services.Communication;

using System.Collections.Concurrent;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;

public sealed class CommunicationService : ICommunicationService
{
    private static readonly TimeSpan MinMessageInterval = TimeSpan.FromMilliseconds(600);
    private static readonly string[] FilteredTerms = ["hate", "slur", "spamlink"];

    private readonly GalacticTraderDbContext _dbContext;
    private readonly ConcurrentDictionary<Guid, DateTime> _lastMessageByPlayer = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _subscriptions = new();

    public CommunicationService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public event Func<ChannelMessageDto, Task>? MessageBroadcast;

    public Task<ChannelSubscriptionDto> SubscribeAsync(SubscribeChannelRequest request, CancellationToken cancellationToken = default)
    {
        var channelId = BuildChannelId(request.ChannelType, request.ChannelKey);
        var channels = _subscriptions.GetOrAdd(request.PlayerId, _ => new ConcurrentDictionary<string, byte>());
        channels[channelId] = 0;

        return Task.FromResult(new ChannelSubscriptionDto
        {
            PlayerId = request.PlayerId,
            ChannelType = request.ChannelType.ToString(),
            ChannelKey = NormalizeChannelKey(request.ChannelKey),
            IsSubscribed = true
        });
    }

    public Task<ChannelSubscriptionDto> UnsubscribeAsync(SubscribeChannelRequest request, CancellationToken cancellationToken = default)
    {
        var channelId = BuildChannelId(request.ChannelType, request.ChannelKey);
        if (_subscriptions.TryGetValue(request.PlayerId, out var channels))
        {
            channels.TryRemove(channelId, out _);
        }

        return Task.FromResult(new ChannelSubscriptionDto
        {
            PlayerId = request.PlayerId,
            ChannelType = request.ChannelType.ToString(),
            ChannelKey = NormalizeChannelKey(request.ChannelKey),
            IsSubscribed = false
        });
    }

    public async Task<ChannelMessageDto?> SendMessageAsync(SendChannelMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return null;
        }

        var senderExists = await _dbContext.Players.AnyAsync(player => player.Id == request.PlayerId, cancellationToken);
        if (!senderExists)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (_lastMessageByPlayer.TryGetValue(request.PlayerId, out var lastSentAt) && (now - lastSentAt) < MinMessageInterval)
        {
            throw new InvalidOperationException("Rate limit exceeded for chat messages.");
        }

        var moderated = ModerateMessage(request.Content, out var wasModerated);
        var message = new ChannelMessage
        {
            Id = Guid.NewGuid(),
            SenderId = request.PlayerId,
            ChannelType = request.ChannelType.ToString(),
            ChannelKey = NormalizeChannelKey(request.ChannelKey),
            Content = moderated,
            IsModerated = wasModerated,
            CreatedAt = now
        };

        _dbContext.ChannelMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _lastMessageByPlayer[request.PlayerId] = now;

        var dto = Map(message);
        var handler = MessageBroadcast;
        if (handler is not null)
        {
            await handler(dto);
        }

        return dto;
    }

    public async Task<IReadOnlyList<ChannelMessageDto>> GetRecentMessagesAsync(
        ChannelType channelType,
        string channelKey,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeChannelKey(channelKey);
        var messages = await _dbContext.ChannelMessages
            .AsNoTracking()
            .Where(message => message.ChannelType == channelType.ToString() && message.ChannelKey == normalizedKey)
            .OrderByDescending(message => message.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(message => message.CreatedAt)
            .Select(Map)
            .ToList();
    }

    public Task<bool> IsSubscribedAsync(Guid playerId, ChannelType channelType, string channelKey, CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryGetValue(playerId, out var channels))
        {
            return Task.FromResult(false);
        }

        var channelId = BuildChannelId(channelType, channelKey);
        return Task.FromResult(channels.ContainsKey(channelId));
    }

    private static string BuildChannelId(ChannelType channelType, string channelKey)
    {
        return $"{channelType}:{NormalizeChannelKey(channelKey)}";
    }

    private static string NormalizeChannelKey(string channelKey)
    {
        return string.IsNullOrWhiteSpace(channelKey) ? "global" : channelKey.Trim().ToLowerInvariant();
    }

    private static ChannelMessageDto Map(ChannelMessage message)
    {
        return new ChannelMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            ChannelType = message.ChannelType,
            ChannelKey = message.ChannelKey,
            Content = message.Content,
            IsModerated = message.IsModerated,
            CreatedAt = message.CreatedAt
        };
    }

    private static string ModerateMessage(string input, out bool wasModerated)
    {
        wasModerated = false;
        var output = input;
        foreach (var term in FilteredTerms)
        {
            if (!output.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            output = output.Replace(term, "***", StringComparison.OrdinalIgnoreCase);
            wasModerated = true;
        }

        return output;
    }
}
