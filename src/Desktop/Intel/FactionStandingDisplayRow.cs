namespace GalacticTrader.Desktop.Intel;

public sealed class FactionStandingDisplayRow
{
    public required string FactionId { get; init; }
    public int ReputationScore { get; init; }
    public required string Tier { get; init; }
    public required string Access { get; init; }
    public decimal TradingDiscount { get; init; }
    public decimal TaxModifier { get; init; }
}
