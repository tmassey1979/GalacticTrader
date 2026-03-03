namespace GalacticTrader.Services.Economy;

public sealed class MarketShockRequest
{
    public Guid MarketId { get; init; }
    public float Intensity { get; init; } = 0.25f;
    public string Reason { get; init; } = "Unspecified";
}
