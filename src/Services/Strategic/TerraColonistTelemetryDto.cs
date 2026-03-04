namespace GalacticTrader.Services.Strategic;

public sealed class TerraColonistTelemetryDto
{
    public Guid? PlayerId { get; init; }
    public int InTransitShipmentCount { get; init; }
    public long ColonistsInTransit { get; init; }
    public int DeliveredLast24HoursCount { get; init; }
    public long ColonistsDeliveredLast24Hours { get; init; }
    public DateTime ObservedAtUtc { get; init; }
}
