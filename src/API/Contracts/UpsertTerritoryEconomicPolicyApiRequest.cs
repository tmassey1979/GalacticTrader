namespace GalacticTrader.API.Contracts;

public sealed class UpsertTerritoryEconomicPolicyApiRequest
{
    public Guid FactionId { get; init; }
    public decimal TaxRatePercent { get; init; }
    public decimal TradeIncentivePercent { get; init; }
}
