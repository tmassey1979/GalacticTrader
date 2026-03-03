namespace GalacticTrader.Services.Communication;

public sealed class SpatialAudioResult
{
    public Guid ListenerId { get; init; }
    public IReadOnlyList<SpeakerMix> Mix { get; init; } = [];
}
