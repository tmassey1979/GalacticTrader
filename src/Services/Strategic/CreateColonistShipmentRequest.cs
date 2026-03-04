using GalacticTrader.Services.Navigation;

namespace GalacticTrader.Services.Strategic;

public sealed class CreateColonistShipmentRequest
{
    public Guid PlayerId { get; init; }
    public Guid DestinationSectorId { get; init; }
    public long ColonistCount { get; init; }
    public TravelMode TravelMode { get; init; } = TravelMode.Standard;
    public string Algorithm { get; init; } = "dijkstra";
}
