namespace GalacticTrader.Desktop.Trading;

public sealed class TradingTransactionFilterOptions
{
    public required string Action { get; init; }
    public string ListingKeyword { get; init; } = string.Empty;
}
