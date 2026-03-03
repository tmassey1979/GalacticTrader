namespace GalacticTrader.Services.Health;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check for database connectivity.
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
