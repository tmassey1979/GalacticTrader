namespace GalacticTrader.Services.Strategic;

public sealed class ColonistDeliveryAuditDto
{
    public Guid Id { get; init; }
    public Guid ShipmentId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid DestinationSectorId { get; init; }
    public string DestinationSectorName { get; init; } = string.Empty;
    public long ColonistCount { get; init; }
    public float EstimatedRiskScore { get; init; }
    public DateTime DeliveredAtUtc { get; init; }
}
