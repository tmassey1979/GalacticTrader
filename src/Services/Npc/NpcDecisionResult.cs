namespace GalacticTrader.Services.Npc;

public sealed class NpcDecisionResult
{
    public Guid AgentId { get; init; }
    public int DecisionTick { get; init; }
    public string PreviousGoal { get; init; } = string.Empty;
    public string CurrentGoal { get; init; } = string.Empty;
    public Guid? PreviousTarget { get; init; }
    public Guid? CurrentTarget { get; init; }
    public float OpportunityScore { get; init; }
}
