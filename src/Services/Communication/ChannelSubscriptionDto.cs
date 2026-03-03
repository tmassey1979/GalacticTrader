namespace GalacticTrader.Services.Communication;

public sealed class ChannelSubscriptionDto
{
    public Guid PlayerId { get; init; }
    public string ChannelType { get; init; } = string.Empty;
    public string ChannelKey { get; init; } = string.Empty;
    public bool IsSubscribed { get; init; }
}
