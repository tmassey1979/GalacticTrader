namespace GalacticTrader.Services.Reputation;

public interface IReputationService
{
    Task<PlayerFactionStandingDto?> AdjustFactionStandingAsync(UpdateFactionStandingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlayerFactionStandingDto>> GetFactionStandingsAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<int> ApplyFactionReputationDecayAsync(int points, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FactionBenefitDto>> GetFactionBenefitsAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task<AlignmentStateDto?> ApplyAlignmentActionAsync(AlignmentActionRequest request, CancellationToken cancellationToken = default);
    Task<AlignmentAccessDto?> GetAlignmentAccessAsync(Guid playerId, CancellationToken cancellationToken = default);
}
