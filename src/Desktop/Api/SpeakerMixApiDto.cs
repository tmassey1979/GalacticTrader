namespace GalacticTrader.Desktop.Api;

public sealed class SpeakerMixApiDto
{
    public Guid PlayerId { get; init; }
    public float Distance { get; init; }
    public float Gain { get; init; }
    public float Pan { get; init; }
}
