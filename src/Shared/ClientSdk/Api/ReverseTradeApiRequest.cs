namespace GalacticTrader.Desktop.Api;

public sealed class ReverseTradeApiRequest
{
    public Guid TradeTransactionId { get; init; }
    public string Reason { get; init; } = "Manual reversal";
}
