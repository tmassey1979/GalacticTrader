namespace GalacticTrader.Services.Navigation;

public sealed class RouteDto
{
    public Guid Id { get; init; }
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public string FromSectorName { get; init; } = string.Empty;
    public string ToSectorName { get; init; } = string.Empty;
    public int TravelTimeSeconds { get; init; }
    public float FuelCost { get; init; }
    public float BaseRiskScore { get; init; }
    public float VisibilityRating { get; init; }
    public string LegalStatus { get; init; } = string.Empty;
    public string WarpGateType { get; init; } = string.Empty;
    public bool IsDiscovered { get; init; }
    public bool HasAnomalies { get; init; }
    public int TrafficIntensity { get; init; }
}
