namespace GalacticTrader.Services.Communication;

public sealed class VoiceActivityDto
{
    public Guid ChannelId { get; init; }
    public Guid PlayerId { get; init; }
    public bool IsSpeaking { get; init; }
    public float VoiceActivityScore { get; init; }
    public DateTime UpdatedAt { get; init; }
}
