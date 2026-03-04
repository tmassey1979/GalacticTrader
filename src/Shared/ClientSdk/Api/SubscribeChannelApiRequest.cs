namespace GalacticTrader.Desktop.Api;

public sealed class SubscribeChannelApiRequest
{
    public Guid PlayerId { get; init; }
    public int ChannelType { get; init; }
    public string ChannelKey { get; init; } = "global";
}
