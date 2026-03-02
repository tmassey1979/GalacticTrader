namespace GalacticTrader.Services.Communication;

public enum ChannelType
{
    Global,
    Sector,
    Faction,
    Private,
    Fleet
}

public sealed class SubscribeChannelRequest
{
    public Guid PlayerId { get; init; }
    public ChannelType ChannelType { get; init; }
    public string ChannelKey { get; init; } = "global";
}

public sealed class SendChannelMessageRequest
{
    public Guid PlayerId { get; init; }
    public ChannelType ChannelType { get; init; } = ChannelType.Global;
    public string ChannelKey { get; init; } = "global";
    public string Content { get; init; } = string.Empty;
}

public sealed class ChannelMessageDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public string ChannelType { get; init; } = string.Empty;
    public string ChannelKey { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsModerated { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ChannelSubscriptionDto
{
    public Guid PlayerId { get; init; }
    public string ChannelType { get; init; } = string.Empty;
    public string ChannelKey { get; init; } = string.Empty;
    public bool IsSubscribed { get; init; }
}
