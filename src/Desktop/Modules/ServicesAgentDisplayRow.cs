namespace GalacticTrader.Desktop.Modules;

public sealed class ServicesAgentDisplayRow
{
    public Guid AgentId { get; init; }
    public required string Name { get; init; }
    public required string Archetype { get; init; }
    public decimal Wealth { get; init; }
    public int FleetSize { get; init; }
    public float InfluenceScore { get; init; }
    public int AggressionIndex { get; init; }
    public required string StrategyBias { get; init; }
    public required string CurrentGoal { get; init; }
}
