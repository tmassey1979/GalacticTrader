namespace GalacticTrader.Desktop.Api;

public sealed class VoiceSignalApiDto
{
    public Guid ChannelId { get; init; }
    public Guid SenderId { get; init; }
    public Guid? TargetPlayerId { get; init; }
    public string SignalType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
