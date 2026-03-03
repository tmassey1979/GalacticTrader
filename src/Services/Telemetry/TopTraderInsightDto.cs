namespace GalacticTrader.Services.Telemetry;

public sealed class TopTraderInsightDto
{
    public Guid PlayerId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal TradeVolume { get; init; }
    public int TradeCount { get; init; }
}
