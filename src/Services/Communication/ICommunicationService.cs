namespace GalacticTrader.Services.Communication;

public interface ICommunicationService
{
    event Func<ChannelMessageDto, Task>? MessageBroadcast;

    Task<ChannelSubscriptionDto> SubscribeAsync(SubscribeChannelRequest request, CancellationToken cancellationToken = default);
    Task<ChannelSubscriptionDto> UnsubscribeAsync(SubscribeChannelRequest request, CancellationToken cancellationToken = default);
    Task<ChannelMessageDto?> SendMessageAsync(SendChannelMessageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChannelMessageDto>> GetRecentMessagesAsync(ChannelType channelType, string channelKey, int limit = 50, CancellationToken cancellationToken = default);
    Task<bool> IsSubscribedAsync(Guid playerId, ChannelType channelType, string channelKey, CancellationToken cancellationToken = default);
}
