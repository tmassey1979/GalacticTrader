namespace GalacticTrader.Services.Npc;

public enum NpcArchetype
{
    Merchant,
    Industrialist,
    ReputableTrader,
    RogueTrader,
    Pirate,
    AlienSyndicate
}

public sealed class CreateNpcRequest
{
    public string Name { get; init; } = string.Empty;
    public NpcArchetype Archetype { get; init; }
    public Guid? FactionId { get; init; }
    public Guid? StartingSectorId { get; init; }
}

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

public sealed class NpcFleetSummary
{
    public Guid AgentId { get; init; }
    public int FleetSize { get; init; }
    public int ActiveShips { get; init; }
    public float CoordinationBonus { get; init; }
    public IReadOnlyList<NpcShipDto> Ships { get; init; } = [];
}

public sealed class NpcShipDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShipClass { get; init; } = string.Empty;
    public int HullIntegrity { get; init; }
    public int CombatRating { get; init; }
    public Guid? CurrentSectorId { get; init; }
    public bool IsActive { get; init; }
}
