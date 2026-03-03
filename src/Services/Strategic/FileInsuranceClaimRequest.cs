namespace GalacticTrader.Services.Strategic;

public sealed class FileInsuranceClaimRequest
{
    public Guid PolicyId { get; init; }
    public Guid? CombatLogId { get; init; }
    public decimal ClaimAmount { get; init; }
}
