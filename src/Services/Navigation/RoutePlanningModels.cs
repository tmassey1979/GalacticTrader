namespace GalacticTrader.Services.Navigation;

public enum TravelMode
{
    Standard,
    HighBurn,
    StealthTransit,
    Convoy,
    GhostRoute,
    ArmedEscort
}

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

public sealed class RoutePlanDto
{
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public string Algorithm { get; init; } = string.Empty;
    public TravelMode TravelMode { get; init; }
    public double TotalCost { get; init; }
    public int TotalTravelTimeSeconds { get; init; }
    public double TotalFuelCost { get; init; }
    public double TotalRiskScore { get; init; }
    public IReadOnlyList<Guid> SectorPath { get; init; } = [];
    public IReadOnlyList<RouteHopDto> Hops { get; init; } = [];
}

public sealed class RouteOptimizationDto
{
    public RoutePlanDto? Fastest { get; init; }
    public RoutePlanDto? Cheapest { get; init; }
    public RoutePlanDto? Safest { get; init; }
    public RoutePlanDto? Balanced { get; init; }
}

internal sealed class TravelModeProfile
{
    public required TravelMode Mode { get; init; }
    public required double TimeMultiplier { get; init; }
    public required double FuelMultiplier { get; init; }
    public required double RiskMultiplier { get; init; }
}
