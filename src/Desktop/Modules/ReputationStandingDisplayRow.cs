namespace GalacticTrader.Desktop.Modules;

public sealed class ReputationStandingDisplayRow
{
    public required string FactionId { get; init; }
    public int ReputationScore { get; init; }
    public required string Tier { get; init; }
    public required string Badge { get; init; }
    public required string AccentHex { get; init; }
    public bool HasAccess { get; init; }
    public decimal TradingDiscount { get; init; }
}
