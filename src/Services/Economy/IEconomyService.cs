namespace GalacticTrader.Services.Economy;

public interface IEconomyService
{
    Task<PriceCalculationResult?> CalculatePriceAsync(
        PriceCalculationInput input,
        CancellationToken cancellationToken = default);

    Task<MarketTickResult> ProcessMarketTickAsync(CancellationToken cancellationToken = default);

    Task<bool> TriggerMarketShockAsync(MarketShockRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommodityHierarchyItem>> GetCommodityHierarchyAsync(CancellationToken cancellationToken = default);
}
