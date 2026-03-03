namespace GalacticTrader.Services.Navigation;

internal sealed class AutopilotSessionInternal
{
    public required Guid SessionId { get; init; }
    public required Guid ShipId { get; init; }
    public required DateTime StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; set; }
    public required RoutePlanDto RoutePlan { get; set; }
    public required AutopilotState State { get; set; }
    public required TravelMode TravelMode { get; set; }
    public required int CurrentHopIndex { get; set; }
    public required int RemainingSecondsInHop { get; set; }
    public int TotalElapsedSeconds { get; set; }
    public decimal CargoValue { get; init; }
    public int PlayerNotoriety { get; init; }
    public int EscortStrength { get; init; }
    public int FactionProtection { get; init; }
    public List<EncounterEventDto> EncounterEvents { get; } = [];
}
