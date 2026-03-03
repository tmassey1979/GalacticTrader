namespace GalacticTrader.Desktop.Api;

public sealed class VoiceChannelApiDto
{
    public Guid ChannelId { get; init; }
    public int Mode { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
    public string EncryptionToken { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
