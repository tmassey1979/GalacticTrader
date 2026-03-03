namespace GalacticTrader.API.Contracts;

public sealed record SectorInstabilityRequest(Guid SectorId, string? Reason);
