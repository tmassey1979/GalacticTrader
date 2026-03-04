namespace GalacticTrader.ClientSdk.Starmap;

public sealed record StarmapSectorNode(
    Guid SectorId,
    string Name,
    MapPoint3 Position,
    bool IsHub = false);
