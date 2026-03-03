namespace GalacticTrader.Services.Communication;

public sealed class SpeakerSample
{
    public Guid PlayerId { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float BaseGain { get; init; } = 1f;
}
