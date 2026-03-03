namespace GalacticTrader.Services.Communication;

public sealed class VoiceActivityRequest
{
    public Guid PlayerId { get; init; }
    public float RmsLevel { get; init; }
    public float PacketLossPercent { get; init; }
    public float LatencyMs { get; init; }
    public float JitterMs { get; init; }
}
