namespace GalacticTrader.API.Contracts;

public sealed record UpdateSectorRequest(int? SecurityLevel, int? HazardRating, Guid? FactionId);
