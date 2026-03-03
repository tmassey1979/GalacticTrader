namespace GalacticTrader.API.Contracts;

public sealed record CreateRouteRequest(Guid FromSectorId, Guid ToSectorId, string LegalStatus, string WarpGateType);
