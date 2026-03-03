namespace GalacticTrader.Services.Navigation;

public sealed class RouteHopDto
{
    public Guid RouteId { get; init; }
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public string FromSectorName { get; init; } = string.Empty;
    public string ToSectorName { get; init; } = string.Empty;
    public int BaseTravelTimeSeconds { get; init; }
    public float BaseFuelCost { get; init; }
    public float BaseRiskScore { get; init; }
}
