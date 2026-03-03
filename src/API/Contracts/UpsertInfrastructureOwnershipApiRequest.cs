namespace GalacticTrader.API.Contracts;

public sealed record UpsertInfrastructureOwnershipApiRequest(Guid SectorId, Guid FactionId, string InfrastructureType, float ControlScore);
