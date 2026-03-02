namespace GalacticTrader.Services.Npc;

public interface INpcService
{
    Task<NpcAgentDto> CreateAgentAsync(CreateNpcRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NpcAgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<NpcAgentDto?> GetAgentAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<NpcDecisionResult?> ProcessDecisionTickAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NpcDecisionResult>> ProcessAllDecisionTicksAsync(CancellationToken cancellationToken = default);
    Task<NpcFleetSummary?> SpawnFleetAsync(Guid agentId, int shipCount, CancellationToken cancellationToken = default);
    Task<bool> PlanRouteAsync(Guid agentId, Guid targetSectorId, CancellationToken cancellationToken = default);
    Task<bool> ProcessFleetMovementAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<decimal?> ExecuteNpcTradeAsync(Guid agentId, CancellationToken cancellationToken = default);
}
