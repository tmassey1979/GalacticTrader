namespace GalacticTrader.Services.Navigation;

public enum AutopilotState
{
    Idle,
    Planning,
    Traveling,
    Paused,
    Encounter,
    Completed,
    Failed,
    Cancelled
}

public sealed class StartAutopilotRequest
{
    public Guid ShipId { get; init; }
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public TravelMode TravelMode { get; init; } = TravelMode.Standard;
    public decimal CargoValue { get; init; }
    public int PlayerNotoriety { get; init; }
    public int EscortStrength { get; init; }
    public int FactionProtection { get; init; }
}

public sealed class TransitionTravelModeRequest
{
    public TravelMode TargetMode { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed class EncounterEventDto
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid SectorId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public double EncounterScore { get; init; }
    public double Probability { get; init; }
    public string Description { get; init; } = string.Empty;
}

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
