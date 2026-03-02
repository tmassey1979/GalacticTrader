namespace GalacticTrader.Services.Navigation;

public interface IAutopilotService
{
    Task<AutopilotSessionDto> StartAutopilotAsync(
        StartAutopilotRequest request,
        CancellationToken cancellationToken = default);

    Task<AutopilotSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutopilotSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    Task<AutopilotTickResultDto?> ProcessTickAsync(
        Guid sessionId,
        int tickSeconds = 1,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutopilotTickResultDto>> ProcessActiveTicksAsync(
        int tickSeconds = 1,
        CancellationToken cancellationToken = default);

    Task<AutopilotSessionDto?> TransitionTravelModeAsync(
        Guid sessionId,
        TravelMode targetMode,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
