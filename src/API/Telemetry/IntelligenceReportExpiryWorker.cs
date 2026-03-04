namespace GalacticTrader.API.Telemetry;

using System.Diagnostics;
using GalacticTrader.Services.Strategic;

internal sealed class IntelligenceReportExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntelligenceReportExpiryWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;

    public IntelligenceReportExpiryWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<IntelligenceReportExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _enabled = configuration.GetValue<bool?>("Strategic:IntelligenceExpiryWorker:Enabled") ?? true;
        var configuredIntervalSeconds =
            configuration.GetValue<int?>("Strategic:IntelligenceExpiryWorker:IntervalSeconds") ?? 300;
        _interval = TimeSpan.FromSeconds(Math.Clamp(configuredIntervalSeconds, 1, 86_400));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Intelligence report expiry worker disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunMaintenanceCycleAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunMaintenanceCycleAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var strategicService = scope.ServiceProvider.GetRequiredService<IStrategicSystemsService>();
            var expired = await strategicService.ExpireIntelligenceReportsAsync(cancellationToken);

            PrometheusMetrics.StrategicIntelligenceExpiryRuns.WithLabels("success").Inc();
            PrometheusMetrics.StrategicIntelligenceReportsExpired.Inc(expired);

            if (expired > 0)
            {
                _logger.LogInformation("Scheduled intelligence expiry worker marked {Expired} reports as expired.", expired);
            }
        }
        catch (Exception exception)
        {
            PrometheusMetrics.StrategicIntelligenceExpiryRuns.WithLabels("failure").Inc();
            _logger.LogWarning(exception, "Scheduled intelligence expiry worker failed.");
        }
        finally
        {
            stopwatch.Stop();
            PrometheusMetrics.StrategicIntelligenceExpiryDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }
}
