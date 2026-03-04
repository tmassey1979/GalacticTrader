namespace GalacticTrader.Desktop.Api;

public sealed class RouteApiDto
{
    public Guid Id { get; init; }
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public string FromSectorName { get; init; } = string.Empty;
    public string ToSectorName { get; init; } = string.Empty;
    public float BaseRiskScore { get; init; }
}
