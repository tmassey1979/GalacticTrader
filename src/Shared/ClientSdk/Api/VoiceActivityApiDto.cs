namespace GalacticTrader.Desktop.Api;

public sealed class VoiceActivityApiDto
{
    public Guid ChannelId { get; init; }
    public Guid PlayerId { get; init; }
    public bool IsSpeaking { get; init; }
    public float VoiceActivityScore { get; init; }
    public DateTime UpdatedAt { get; init; }
}
