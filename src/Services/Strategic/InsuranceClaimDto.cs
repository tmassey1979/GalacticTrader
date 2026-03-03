namespace GalacticTrader.Services.Strategic;

public sealed class InsuranceClaimDto
{
    public Guid Id { get; init; }
    public Guid PolicyId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public decimal ClaimAmount { get; init; }
    public float FraudRiskScore { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime FiledAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
