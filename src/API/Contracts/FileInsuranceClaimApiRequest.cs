namespace GalacticTrader.API.Contracts;

public sealed record FileInsuranceClaimApiRequest(Guid PolicyId, Guid? CombatLogId, decimal ClaimAmount);
