namespace GalacticTrader.Services.Communication;

public sealed class SendChannelMessageRequest
{
    public Guid PlayerId { get; init; }
    public ChannelType ChannelType { get; init; } = ChannelType.Global;
    public string ChannelKey { get; init; } = "global";
    public string Content { get; init; } = string.Empty;
}
