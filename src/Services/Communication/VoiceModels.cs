namespace GalacticTrader.Services.Communication;

public enum VoiceMode
{
    Proximity,
    Fleet,
    EncryptedPrivate
}

public sealed class CreateVoiceChannelRequest
{
    public Guid CreatorPlayerId { get; init; }
    public VoiceMode Mode { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
}

public sealed class JoinVoiceChannelRequest
{
    public Guid PlayerId { get; init; }
}

public sealed class VoiceChannelDto
{
    public Guid ChannelId { get; init; }
    public VoiceMode Mode { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
    public string EncryptionToken { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class VoiceSignalRequest
{
    public Guid SenderId { get; init; }
    public Guid? TargetPlayerId { get; init; }
    public string SignalType { get; init; } = string.Empty; // offer, answer, candidate
    public string Payload { get; init; } = string.Empty;
}

public sealed class VoiceSignalDto
{
    public Guid ChannelId { get; init; }
    public Guid SenderId { get; init; }
    public Guid? TargetPlayerId { get; init; }
    public string SignalType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class VoiceActivityRequest
{
    public Guid PlayerId { get; init; }
    public float RmsLevel { get; init; }
    public float PacketLossPercent { get; init; }
    public float LatencyMs { get; init; }
    public float JitterMs { get; init; }
}

public sealed class VoiceActivityDto
{
    public Guid ChannelId { get; init; }
    public Guid PlayerId { get; init; }
    public bool IsSpeaking { get; init; }
    public float VoiceActivityScore { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class SpatialAudioRequest
{
    public Guid ListenerId { get; init; }
    public float ListenerX { get; init; }
    public float ListenerY { get; init; }
    public float ListenerZ { get; init; }
    public float FalloffDistance { get; init; } = 100f;
    public IReadOnlyList<SpeakerSample> Speakers { get; init; } = [];
}

public sealed class SpeakerSample
{
    public Guid PlayerId { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float BaseGain { get; init; } = 1f;
}

public sealed class SpatialAudioResult
{
    public Guid ListenerId { get; init; }
    public IReadOnlyList<SpeakerMix> Mix { get; init; } = [];
}

public sealed class SpeakerMix
{
    public Guid PlayerId { get; init; }
    public float Distance { get; init; }
    public float Gain { get; init; }
    public float Pan { get; init; }
}

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
