namespace GalacticTrader.Services.Telemetry;

public interface IMarketIntelligenceService
{
    Task<MarketIntelligenceSummaryDto> GetSummaryAsync(int limit = 8, CancellationToken cancellationToken = default);
}
