namespace GalacticTrader.ClientSdk.Starmap;

public sealed record StarmapSectorRenderPlan(
    Guid SectorId,
    string Name,
    MapPoint3 Position,
    StarmapLodTier LodTier,
    double DistanceFromCamera,
    bool IsHub);
