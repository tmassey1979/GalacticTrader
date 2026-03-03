namespace GalacticTrader.Services.Strategic;

public sealed class InsurancePolicyDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public string ShipName { get; init; } = string.Empty;
    public float CoverageRate { get; init; }
    public decimal PremiumPerCycle { get; init; }
    public string RiskTier { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastPremiumChargedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
