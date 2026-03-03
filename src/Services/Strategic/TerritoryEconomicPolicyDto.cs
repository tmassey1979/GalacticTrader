namespace GalacticTrader.Services.Strategic;

public sealed class TerritoryEconomicPolicyDto
{
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public decimal TaxRate { get; init; }
    public decimal TradeIncentiveModifier { get; init; }
    public DateTime ObservedAtUtc { get; init; }
}
