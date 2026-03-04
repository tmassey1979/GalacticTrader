namespace GalacticTrader.Services.Strategic;

public sealed class ColonistShipmentDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public Guid FromSectorId { get; init; }
    public string FromSectorName { get; init; } = string.Empty;
    public Guid DestinationSectorId { get; init; }
    public string DestinationSectorName { get; init; } = string.Empty;
    public long ColonistCount { get; init; }
    public int RouteTravelSeconds { get; init; }
    public float EstimatedRiskScore { get; init; }
    public string TravelMode { get; init; } = string.Empty;
    public DateTime LoadedAtUtc { get; init; }
    public DateTime EstimatedArrivalAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
}
