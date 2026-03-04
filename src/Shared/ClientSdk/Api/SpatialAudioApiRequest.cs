namespace GalacticTrader.Desktop.Api;

public sealed class SpatialAudioApiRequest
{
    public Guid ListenerId { get; init; }
    public float ListenerX { get; init; }
    public float ListenerY { get; init; }
    public float ListenerZ { get; init; }
    public float FalloffDistance { get; init; } = 100f;
    public IReadOnlyList<SpeakerSampleApiRequest> Speakers { get; init; } = [];
}
