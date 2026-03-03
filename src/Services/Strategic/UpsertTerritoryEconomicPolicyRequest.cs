namespace GalacticTrader.Services.Strategic;

public sealed class UpsertTerritoryEconomicPolicyRequest
{
    public Guid FactionId { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TradeIncentiveModifier { get; init; }
}
