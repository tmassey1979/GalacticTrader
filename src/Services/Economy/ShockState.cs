namespace GalacticTrader.Services.Economy;

internal sealed class ShockState
{
    public required Guid MarketId { get; init; }
    public required float Multiplier { get; init; }
    public required string Reason { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}
