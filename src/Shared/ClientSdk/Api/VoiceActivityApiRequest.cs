namespace GalacticTrader.Desktop.Api;

public sealed class VoiceActivityApiRequest
{
    public Guid PlayerId { get; init; }
    public float RmsLevel { get; init; }
    public float PacketLossPercent { get; init; }
    public float LatencyMs { get; init; }
    public float JitterMs { get; init; }
}
