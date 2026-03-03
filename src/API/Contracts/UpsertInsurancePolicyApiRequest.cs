namespace GalacticTrader.API.Contracts;

public sealed record UpsertInsurancePolicyApiRequest(Guid PlayerId, Guid ShipId, float CoverageRate, decimal PremiumPerCycle, string RiskTier, bool IsActive);
