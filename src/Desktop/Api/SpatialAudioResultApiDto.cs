namespace GalacticTrader.Desktop.Api;

public sealed class SpatialAudioResultApiDto
{
    public Guid ListenerId { get; init; }
    public IReadOnlyList<SpeakerMixApiDto> Mix { get; init; } = [];
}
