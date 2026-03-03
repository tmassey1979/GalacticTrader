namespace GalacticTrader.Data.Models;

public sealed class InsuranceClaim
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? CombatLogId { get; set; }
    public decimal ClaimAmount { get; set; }
    public float FraudRiskScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime FiledAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public InsurancePolicy? Policy { get; set; }
    public CombatLog? CombatLog { get; set; }
}
