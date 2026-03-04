namespace GalacticTrader.Desktop.Api;

public sealed class CommunicationChannelMessageApiDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public string ChannelType { get; init; } = string.Empty;
    public string ChannelKey { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsModerated { get; init; }
    public DateTime CreatedAt { get; init; }
}
