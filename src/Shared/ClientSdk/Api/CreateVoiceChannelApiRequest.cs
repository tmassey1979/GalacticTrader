namespace GalacticTrader.Desktop.Api;

public sealed class CreateVoiceChannelApiRequest
{
    public Guid CreatorPlayerId { get; init; }
    public int Mode { get; init; }
    public string ScopeKey { get; init; } = string.Empty;
}
