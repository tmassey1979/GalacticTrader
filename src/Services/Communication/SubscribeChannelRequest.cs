namespace GalacticTrader.Services.Communication;

public sealed class SubscribeChannelRequest
{
    public Guid PlayerId { get; init; }
    public ChannelType ChannelType { get; init; }
    public string ChannelKey { get; init; } = "global";
}
