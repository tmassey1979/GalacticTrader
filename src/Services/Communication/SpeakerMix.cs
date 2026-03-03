namespace GalacticTrader.Services.Communication;

public sealed class SpeakerMix
{
    public Guid PlayerId { get; init; }
    public float Distance { get; init; }
    public float Gain { get; init; }
    public float Pan { get; init; }
}
