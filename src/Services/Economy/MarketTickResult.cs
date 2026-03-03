namespace GalacticTrader.Services.Economy;

public sealed class MarketTickResult
{
    public DateTime ProcessedAtUtc { get; init; }
    public int MarketsProcessed { get; init; }
    public int ListingsUpdated { get; init; }
    public int ShockEventsTriggered { get; init; }
}
