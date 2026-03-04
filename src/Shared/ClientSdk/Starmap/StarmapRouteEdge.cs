namespace GalacticTrader.ClientSdk.Starmap;

public sealed record StarmapRouteEdge(
    Guid RouteId,
    Guid FromSectorId,
    Guid ToSectorId,
    bool IsHighRisk = false);
