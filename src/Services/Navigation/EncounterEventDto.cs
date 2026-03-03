namespace GalacticTrader.Services.Navigation;

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
