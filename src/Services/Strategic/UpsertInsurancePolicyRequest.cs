namespace GalacticTrader.Services.Strategic;

public sealed class UpsertInsurancePolicyRequest
{
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public float CoverageRate { get; init; }
    public decimal PremiumPerCycle { get; init; }
    public string RiskTier { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}
