namespace GalacticTrader.Services.Combat;

public interface ICombatService
{
    Task<CombatSummaryDto> StartCombatAsync(StartCombatRequest request, CancellationToken cancellationToken = default);
    Task<CombatSummaryDto?> GetCombatAsync(Guid combatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CombatSummaryDto>> GetActiveCombatsAsync(CancellationToken cancellationToken = default);
    Task<CombatTickResultDto?> ProcessTickAsync(Guid combatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CombatTickResultDto>> ProcessTicksAsync(
        Guid combatId,
        int tickCount,
        CancellationToken cancellationToken = default);
    Task<CombatSummaryDto?> EndCombatAsync(Guid combatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CombatLogDto>> GetRecentCombatLogsAsync(int limit = 50, CancellationToken cancellationToken = default);
}
