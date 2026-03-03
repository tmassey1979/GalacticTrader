namespace GalacticTrader.Services.Fleet;

public sealed class ConvoySimulationResult
{
    public EscortSummaryDto Summary { get; init; } = new();
    public int ExpectedLossPercent { get; init; }
    public decimal ProjectedProtectedValue { get; init; }
}
