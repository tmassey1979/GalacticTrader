namespace GalacticTrader.API.Telemetry;

using GalacticTrader.Data;
using GalacticTrader.Services.Caching;
using Microsoft.EntityFrameworkCore;

internal sealed class TelemetryGaugeRefreshService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelemetryGaugeRefreshService> _logger;

    public TelemetryGaugeRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<TelemetryGaugeRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshMetricsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Telemetry gauge refresh failed.");
            }

            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }

    private async Task RefreshMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GalacticTraderDbContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var activeCutoff = DateTime.UtcNow.AddMinutes(-15);
        var activeUsers = await dbContext.Players
            .AsNoTracking()
            .CountAsync(player => player.IsActive && player.LastActiveAt >= activeCutoff, cancellationToken);

        var activeBattles = await dbContext.Ships
            .AsNoTracking()
            .CountAsync(ship => ship.IsInCombat, cancellationToken);

        var playerCurrency = await dbContext.Players
            .AsNoTracking()
            .Select(player => (decimal?)player.LiquidCredits)
            .SumAsync(cancellationToken) ?? 0m;

        var factionTreasury = await dbContext.Factions
            .AsNoTracking()
            .Select(faction => (decimal?)faction.TreasuryBalance)
            .SumAsync(cancellationToken) ?? 0m;

        PrometheusMetrics.ActiveUsers.Set(activeUsers);
        PrometheusMetrics.ActiveBattles.Set(activeBattles);
        PrometheusMetrics.TotalCurrencyInCirculation.Set((double)(playerCurrency + factionTreasury));

        if (cacheService is ICacheMetricsProvider cacheMetricsProvider)
        {
            PrometheusMetrics.RedisCacheHitRatio.Set(cacheMetricsProvider.HitRatio);
        }
    }
}
