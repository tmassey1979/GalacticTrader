namespace GalacticTrader.Desktop.Api;

public sealed class ConvoySimulationResultApiDto
{
    public EscortSummaryApiDto Summary { get; init; } = new();
    public int ExpectedLossPercent { get; init; }
    public decimal ProjectedProtectedValue { get; init; }
}
