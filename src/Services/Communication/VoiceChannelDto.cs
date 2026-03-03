namespace GalacticTrader.Services.Communication;

public sealed class VoiceChannelDto
{
    public Guid ChannelId { get; init; }
    public VoiceMode Mode { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
    public string EncryptionToken { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
