namespace GalacticTrader.Services.Communication;

public sealed class CreateVoiceChannelRequest
{
    public Guid CreatorPlayerId { get; init; }
    public VoiceMode Mode { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
}
