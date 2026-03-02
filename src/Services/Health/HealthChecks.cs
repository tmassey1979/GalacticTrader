using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace GalacticTrader.Services.Health
{
    /// <summary>
    /// Health check for Redis connectivity
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisHealthCheck(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var ping = await server.PingAsync();

                if (ping == TimeSpan.Zero)
                {
                    return HealthCheckResult.Unhealthy("Redis ping returned zero response time");
                }

                var data = new Dictionary<string, object>
                {
                    { "ping_ms", ping.TotalMilliseconds },
                    { "connected_endpoints", _redis.GetEndPoints().Length }
                };

                return HealthCheckResult.Healthy("Redis is operational", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Redis health check failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Health check for database connectivity
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly GalacticTrader.Data.GalacticTraderDbContext _context;

        public DatabaseHealthCheck(GalacticTrader.Data.GalacticTraderDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                if (!canConnect)
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to database");
                }

                return HealthCheckResult.Healthy("Database connection is operational");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}");
            }
        }
    }
}
