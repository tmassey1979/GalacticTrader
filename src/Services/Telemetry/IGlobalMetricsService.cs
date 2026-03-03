namespace GalacticTrader.Services.Telemetry;

public interface IGlobalMetricsService
{
    Task<GlobalMetricsSummaryDto> GetGlobalSummaryAsync(CancellationToken cancellationToken = default);
}
