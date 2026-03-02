namespace GalacticTrader.Services.Navigation;

public sealed class SectorDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public int SecurityLevel { get; init; }
    public int HazardRating { get; init; }
    public float ResourceModifier { get; init; }
    public int EconomicIndex { get; init; }
    public float SensorInterferenceLevel { get; init; }
    public Guid? ControlledByFactionId { get; init; }
    public string? ControlledByFactionName { get; init; }
    public int ConnectedSectorCount { get; init; }
}

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

public sealed class GraphValidationReport
{
    public int SectorCount { get; init; }
    public int RouteCount { get; init; }
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
