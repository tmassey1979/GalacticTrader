namespace GalacticTrader.Services.Navigation;

public sealed class AutopilotTickResultDto
{
    public Guid SessionId { get; init; }
    public AutopilotState State { get; init; }
    public int CurrentHopIndex { get; init; }
    public int TotalHops { get; init; }
    public int RemainingSecondsInHop { get; init; }
    public int TotalElapsedSeconds { get; init; }
    public int TotalRemainingSeconds { get; init; }
    public TravelMode CurrentMode { get; init; }
    public List<EncounterEventDto> NewEvents { get; init; } = [];
}
