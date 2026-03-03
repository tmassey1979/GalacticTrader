namespace GalacticTrader.Services.Reputation;

public sealed class PlayerFactionStandingDto
{
    public Guid PlayerId { get; init; }
    public Guid FactionId { get; init; }
    public int ReputationScore { get; init; }
    public string Tier { get; init; } = string.Empty;
    public bool HasAccess { get; init; }
    public decimal TradingDiscount { get; init; }
    public decimal TaxModifier { get; init; }
    public IReadOnlyList<string> Benefits { get; init; } = [];
}
