namespace GalacticTrader.Services.Market;

public sealed class ReverseTradeRequest
{
    public Guid TradeTransactionId { get; init; }
    public string Reason { get; init; } = "Manual reversal";
}
