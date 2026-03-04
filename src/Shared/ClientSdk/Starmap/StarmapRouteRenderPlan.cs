namespace GalacticTrader.ClientSdk.Starmap;

public sealed record StarmapRouteRenderPlan(
    Guid RouteId,
    Guid FromSectorId,
    Guid ToSectorId,
    StarmapLodTier LodTier,
    double DistanceFromCamera,
    bool IsHighRisk);
