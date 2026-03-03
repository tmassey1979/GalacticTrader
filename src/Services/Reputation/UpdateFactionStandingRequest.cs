namespace GalacticTrader.Services.Reputation;

public sealed class UpdateFactionStandingRequest
{
    public Guid PlayerId { get; init; }
    public Guid FactionId { get; init; }
    public int ReputationDelta { get; init; }
    public string Reason { get; init; } = string.Empty;
}
