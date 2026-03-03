namespace GalacticTrader.Desktop.Modules;

public sealed class ServicesStrategyBiasDistributionEntry
{
    public required string StrategyBias { get; init; }
    public int AgentCount { get; init; }
    public double SharePercent { get; init; }
    public required string ShareSummary { get; init; }
}
