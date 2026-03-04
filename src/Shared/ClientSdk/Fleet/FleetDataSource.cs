using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Fleet;

public sealed class FleetDataSource
{
    public required Func<CancellationToken, Task<IReadOnlyList<ShipTemplateApiDto>>> LoadShipTemplatesAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<IReadOnlyList<ShipApiDto>>> LoadShipsAsync { get; init; }

    public required Func<Guid, string, CancellationToken, Task<EscortSummaryApiDto?>> LoadEscortSummaryAsync { get; init; }

    public required Func<PurchaseShipApiRequest, CancellationToken, Task<ShipApiDto>> PurchaseShipAsync { get; init; }

    public required Func<ConvoySimulationApiRequest, CancellationToken, Task<ConvoySimulationResultApiDto?>> SimulateConvoyAsync { get; init; }
}
