using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Routes;

public sealed record RouteModuleState(
    IReadOnlyList<SectorApiDto> Sectors,
    IReadOnlyList<RouteApiDto> Routes,
    IReadOnlyList<RouteApiDto> DangerousRoutes,
    IReadOnlyList<RouteSectorOption> SectorOptions,
    RouteOverlayState Overlay,
    DateTime LoadedAtUtc);
