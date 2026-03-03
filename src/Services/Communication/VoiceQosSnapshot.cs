namespace GalacticTrader.Services.Communication;

public sealed class VoiceQosSnapshot
{
    public Guid ChannelId { get; init; }
    public int ParticipantCount { get; init; }
    public float AverageLatencyMs { get; init; }
    public float AverageJitterMs { get; init; }
    public float AveragePacketLossPercent { get; init; }
    public int SpeakingParticipants { get; init; }
    public DateTime SampledAt { get; init; }
}
