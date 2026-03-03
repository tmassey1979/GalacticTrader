namespace GalacticTrader.Data.Models;

public sealed class InsurancePolicy
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid ShipId { get; set; }
    public float CoverageRate { get; set; }
    public decimal PremiumPerCycle { get; set; }
    public string RiskTier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastPremiumChargedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Player? Player { get; set; }
    public Ship? Ship { get; set; }
}
