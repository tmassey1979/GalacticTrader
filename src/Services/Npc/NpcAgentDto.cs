namespace GalacticTrader.Services.Npc;

public sealed class NpcAgentDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Archetype { get; init; } = string.Empty;
    public decimal Wealth { get; init; }
    public int ReputationScore { get; init; }
    public int FleetSize { get; init; }
    public float RiskTolerance { get; init; }
    public float InfluenceScore { get; init; }
    public string CurrentGoal { get; init; } = string.Empty;
    public Guid? CurrentLocationId { get; init; }
    public Guid? TargetLocationId { get; init; }
    public int DecisionTick { get; init; }
}
