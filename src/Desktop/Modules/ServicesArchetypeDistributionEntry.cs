namespace GalacticTrader.Desktop.Modules;

public sealed class ServicesArchetypeDistributionEntry
{
    public required string Archetype { get; init; }
    public int AgentCount { get; init; }
    public double SharePercent { get; init; }
    public required string ShareSummary { get; init; }
}
