using GalacticTrader.Data;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Services.Telemetry;

public sealed class GlobalMetricsService : IGlobalMetricsService
{
    private readonly GalacticTraderDbContext _dbContext;

    public GlobalMetricsService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GlobalMetricsSummaryDto> GetGlobalSummaryAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var windowStartUtc = nowUtc.AddHours(-24);

        var totalUsersTask = _dbContext.Players.AsNoTracking().CountAsync(cancellationToken);
        var activeUsersTask = _dbContext.Players
            .AsNoTracking()
            .CountAsync(player => player.IsActive && player.LastActiveAt >= windowStartUtc, cancellationToken);
        var topReputationTask = _dbContext.Players
            .AsNoTracking()
            .OrderByDescending(player => player.ReputationScore)
            .Select(player => new GlobalTopPlayerDto
            {
                Username = player.Username,
                Score = player.ReputationScore
            })
            .FirstOrDefaultAsync(cancellationToken);
        var topFinancialTask = _dbContext.Players
            .AsNoTracking()
            .OrderByDescending(player => player.NetWorth)
            .Select(player => new GlobalTopPlayerDto
            {
                Username = player.Username,
                Score = player.NetWorth
            })
            .FirstOrDefaultAsync(cancellationToken);
        var battlesTask = _dbContext.CombatLogs
            .AsNoTracking()
            .CountAsync(log => log.BattleEndedAt >= windowStartUtc, cancellationToken);
        var tradeTotalsTask = _dbContext.TradeTransactions
            .AsNoTracking()
            .Where(transaction => transaction.CompletedAt >= windowStartUtc)
            .Select(transaction => transaction.TotalPrice)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalUsersTask, activeUsersTask, topReputationTask, topFinancialTask, battlesTask, tradeTotalsTask);

        var topReputation = topReputationTask.Result ?? new GlobalTopPlayerDto();
        var topFinancial = topFinancialTask.Result ?? new GlobalTopPlayerDto();
        var battlesPerHour = Math.Round(battlesTask.Result / 24m, 2);
        var economicStabilityIndex = ComputeEconomicStabilityIndex(tradeTotalsTask.Result);

        return new GlobalMetricsSummaryDto
        {
            TotalUsers = totalUsersTask.Result,
            ActivePlayers24h = activeUsersTask.Result,
            AvgBattlesPerHour = battlesPerHour,
            EconomicStabilityIndex = economicStabilityIndex,
            TopReputationPlayer = topReputation,
            TopFinancialPlayer = topFinancial
        };
    }

    private static decimal ComputeEconomicStabilityIndex(IReadOnlyList<decimal> tradeTotals)
    {
        if (tradeTotals.Count < 2)
        {
            return 100m;
        }

        var mean = tradeTotals.Average(static value => (double)value);
        if (mean <= 0d)
        {
            return 100m;
        }

        var variance = tradeTotals
            .Select(value =>
            {
                var delta = (double)value - mean;
                return delta * delta;
            })
            .Average();
        var standardDeviation = Math.Sqrt(variance);
        var coefficientOfVariation = standardDeviation / mean;
        var stability = 100d - (coefficientOfVariation * 100d);
        return Math.Round(Math.Clamp((decimal)stability, 0m, 100m), 1);
    }
}
