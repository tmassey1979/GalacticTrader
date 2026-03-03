using GalacticTrader.Data;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.Services.Telemetry;

public sealed class MarketIntelligenceService : IMarketIntelligenceService
{
    private readonly GalacticTraderDbContext _dbContext;

    public MarketIntelligenceService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MarketIntelligenceSummaryDto> GetSummaryAsync(int limit = 8, CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 3, 20);
        var windowStartUtc = DateTime.UtcNow.AddDays(-7);
        var transactions = _dbContext.TradeTransactions
            .AsNoTracking()
            .Where(transaction => transaction.CompletedAt >= windowStartUtc);

        var totalsTask = transactions
            .Select(transaction => transaction.TotalPrice)
            .ToListAsync(cancellationToken);

        var heatmapTask = (
            from transaction in transactions
            join market in _dbContext.Markets.AsNoTracking()
                on transaction.ToMarketId equals market.Id
            join sector in _dbContext.Sectors.AsNoTracking()
                on market.SectorId equals sector.Id
            group transaction by new { sector.Id, sector.Name } into grouped
            orderby grouped.Sum(item => item.TotalPrice) descending
            select new MarketHeatmapPointDto
            {
                SectorId = grouped.Key.Id,
                SectorName = grouped.Key.Name,
                TradeVolume = grouped.Sum(item => item.TotalPrice),
                TradeCount = grouped.Count()
            })
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        var topTradersTask = (
            from transaction in transactions
            join player in _dbContext.Players.AsNoTracking()
                on transaction.PlayerId equals player.Id
            group transaction by new { player.Id, player.Username } into grouped
            orderby grouped.Sum(item => item.TotalPrice) descending
            select new TopTraderInsightDto
            {
                PlayerId = grouped.Key.Id,
                Username = grouped.Key.Username,
                TradeVolume = grouped.Sum(item => item.TotalPrice),
                TradeCount = grouped.Count()
            })
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        var smugglingTask = (
            from transaction in transactions.Where(item => item.UsedSmugglingRoute)
            join fromMarket in _dbContext.Markets.AsNoTracking()
                on transaction.FromMarketId equals fromMarket.Id
            join toMarket in _dbContext.Markets.AsNoTracking()
                on transaction.ToMarketId equals toMarket.Id
            join fromSector in _dbContext.Sectors.AsNoTracking()
                on fromMarket.SectorId equals fromSector.Id
            join toSector in _dbContext.Sectors.AsNoTracking()
                on toMarket.SectorId equals toSector.Id
            group transaction by new { From = fromSector.Name, To = toSector.Name } into grouped
            orderby grouped.Count() descending
            select new SmugglingCorridorInsightDto
            {
                FromSectorName = grouped.Key.From,
                ToSectorName = grouped.Key.To,
                SmugglingRuns = grouped.Count(),
                AverageTradeValue = grouped.Average(item => item.TotalPrice)
            })
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(totalsTask, heatmapTask, topTradersTask, smugglingTask);

        return new MarketIntelligenceSummaryDto
        {
            VolatilityIndex = ComputeVolatilityIndex(totalsTask.Result),
            RegionalHeatmap = heatmapTask.Result,
            TopTraders = topTradersTask.Result,
            SmugglingCorridors = smugglingTask.Result
        };
    }

    private static decimal ComputeVolatilityIndex(IReadOnlyList<decimal> totals)
    {
        if (totals.Count < 2)
        {
            return 0m;
        }

        var mean = totals.Average(static total => (double)total);
        if (mean <= 0d)
        {
            return 0m;
        }

        var variance = totals
            .Select(total =>
            {
                var delta = (double)total - mean;
                return delta * delta;
            })
            .Average();
        var stdDev = Math.Sqrt(variance);
        var volatility = (stdDev / mean) * 100d;
        return Math.Round(Math.Clamp((decimal)volatility, 0m, 200m), 1);
    }
}
