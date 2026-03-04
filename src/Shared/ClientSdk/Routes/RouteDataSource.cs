using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Routes;

public sealed class RouteDataSource
{
    public required Func<CancellationToken, Task<IReadOnlyList<SectorApiDto>>> LoadSectorsAsync { get; init; }

    public required Func<CancellationToken, Task<IReadOnlyList<RouteApiDto>>> LoadRoutesAsync { get; init; }

    public required Func<int, CancellationToken, Task<IReadOnlyList<RouteApiDto>>> LoadDangerousRoutesAsync { get; init; }

    public required Func<Guid, Guid, string, string, CancellationToken, Task<RoutePlanApiDto?>> LoadRoutePlanAsync { get; init; }

    public required Func<Guid, Guid, CancellationToken, Task<RouteOptimizationApiDto>> LoadRouteOptimizationAsync { get; init; }
}
