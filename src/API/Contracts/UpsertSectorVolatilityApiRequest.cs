namespace GalacticTrader.API.Contracts;

public sealed record UpsertSectorVolatilityApiRequest(Guid SectorId, string CurrentPhase, float VolatilityIndex, DateTime? NextTransitionAt);
