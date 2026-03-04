using GalacticTrader.Services.Navigation;

namespace GalacticTrader.API.Contracts;

public sealed class CreateColonistShipmentApiRequest
{
    public Guid PlayerId { get; init; }
    public Guid DestinationSectorId { get; init; }
    public long ColonistCount { get; init; }
    public TravelMode TravelMode { get; init; } = TravelMode.Standard;
    public string Algorithm { get; init; } = "dijkstra";
}
