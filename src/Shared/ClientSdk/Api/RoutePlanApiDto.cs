namespace GalacticTrader.Desktop.Api;

public sealed class RoutePlanApiDto
{
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public string Algorithm { get; init; } = string.Empty;
    public int TravelMode { get; init; }
    public double TotalCost { get; init; }
    public int TotalTravelTimeSeconds { get; init; }
    public double TotalFuelCost { get; init; }
    public double TotalRiskScore { get; init; }
    public IReadOnlyList<Guid> SectorPath { get; init; } = [];
    public IReadOnlyList<RouteHopApiDto> Hops { get; init; } = [];
}
