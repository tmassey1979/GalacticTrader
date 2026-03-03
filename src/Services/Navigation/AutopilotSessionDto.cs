namespace GalacticTrader.Services.Navigation;

public sealed class AutopilotSessionDto
{
    public Guid SessionId { get; init; }
    public Guid ShipId { get; init; }
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public AutopilotState State { get; init; }
    public TravelMode TravelMode { get; init; }
    public RoutePlanDto RoutePlan { get; init; } = new();
    public int CurrentHopIndex { get; init; }
    public int TotalElapsedSeconds { get; init; }
    public int RemainingSecondsInHop { get; init; }
    public int TotalRemainingSeconds { get; init; }
    public decimal CargoValue { get; init; }
    public int PlayerNotoriety { get; init; }
    public int EscortStrength { get; init; }
    public int FactionProtection { get; init; }
    public List<EncounterEventDto> EncounterEvents { get; init; } = [];
}
