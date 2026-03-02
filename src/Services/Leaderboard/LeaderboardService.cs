namespace GalacticTrader.Services.Leaderboard;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;
using LeaderboardEntity = GalacticTrader.Data.Models.Leaderboard;

public sealed class LeaderboardService : ILeaderboardService
{
    private static readonly string[] SupportedTypes = ["wealth", "reputation", "combat", "trade"];

    private readonly GalacticTraderDbContext _dbContext;

    public LeaderboardService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> RecalculateAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<LeaderboardEntryDto>();
        foreach (var leaderboardType in SupportedTypes)
        {
            var batch = await RecalculateByTypeAsync(leaderboardType, cancellationToken);
            results.AddRange(batch);
        }

        return results;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(
        string leaderboardType,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeType(leaderboardType);
        var latest = await _dbContext.Leaderboards
            .AsNoTracking()
            .Where(entry => entry.LeaderboardType == normalizedType)
            .Select(entry => (DateTime?)entry.LastUpdated)
            .MaxAsync(cancellationToken);

        if (!latest.HasValue)
        {
            return [];
        }

        var entries = await _dbContext.Leaderboards
            .AsNoTracking()
            .Include(entry => entry.Player)
            .Where(entry => entry.LeaderboardType == normalizedType && entry.LastUpdated == latest.Value)
            .OrderBy(entry => entry.Rank)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(cancellationToken);

        return entries.Select(MapEntry).ToList();
    }

    public async Task<LeaderboardPositionDto?> GetPlayerPositionAsync(
        Guid playerId,
        string leaderboardType,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeType(leaderboardType);
        var latest = await _dbContext.Leaderboards
            .AsNoTracking()
            .Where(entry => entry.LeaderboardType == normalizedType)
            .Select(entry => (DateTime?)entry.LastUpdated)
            .MaxAsync(cancellationToken);

        if (!latest.HasValue)
        {
            return null;
        }

        var total = await _dbContext.Leaderboards
            .AsNoTracking()
            .CountAsync(entry => entry.LeaderboardType == normalizedType && entry.LastUpdated == latest.Value, cancellationToken);

        var entry = await _dbContext.Leaderboards
            .AsNoTracking()
            .Where(existing =>
                existing.PlayerId == playerId &&
                existing.LeaderboardType == normalizedType &&
                existing.LastUpdated == latest.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return null;
        }

        return new LeaderboardPositionDto
        {
            PlayerId = entry.PlayerId,
            LeaderboardType = normalizedType,
            Rank = entry.Rank,
            TotalPlayers = total,
            Score = entry.Score
        };
    }

    public async Task<IReadOnlyList<LeaderboardHistoryPointDto>> GetHistoryAsync(
        Guid playerId,
        string leaderboardType,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeType(leaderboardType);
        var history = await _dbContext.Leaderboards
            .AsNoTracking()
            .Where(entry => entry.PlayerId == playerId && entry.LeaderboardType == normalizedType)
            .OrderByDescending(entry => entry.LastUpdated)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(entry => new LeaderboardHistoryPointDto
            {
                SnapshotAt = entry.LastUpdated,
                Rank = entry.Rank,
                Score = entry.Score
            })
            .ToListAsync(cancellationToken);

        return history;
    }

    public async Task<int> ResetLeaderboardAsync(string leaderboardType, CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeType(leaderboardType);
        var entries = await _dbContext.Leaderboards
            .Where(entry => entry.LeaderboardType == normalizedType)
            .ToListAsync(cancellationToken);

        _dbContext.Leaderboards.RemoveRange(entries);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> RecalculateByTypeAsync(string leaderboardType, CancellationToken cancellationToken)
    {
        var previousScores = await _dbContext.Leaderboards
            .AsNoTracking()
            .Where(entry => entry.LeaderboardType == leaderboardType)
            .GroupBy(entry => entry.PlayerId)
            .Select(group => group.OrderByDescending(item => item.LastUpdated).First())
            .ToDictionaryAsync(entry => entry.PlayerId, entry => entry.Score, cancellationToken);

        var rankedPlayers = await LoadScoresAsync(leaderboardType, cancellationToken);
        var now = DateTime.UtcNow;

        var entries = rankedPlayers
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.PlayerId)
            .Select((item, index) => new LeaderboardEntity
            {
                Id = Guid.NewGuid(),
                PlayerId = item.PlayerId,
                LeaderboardType = leaderboardType,
                Rank = index + 1,
                Score = decimal.Round(item.Score, 2),
                PreviousScore = previousScores.TryGetValue(item.PlayerId, out var previous) ? previous : 0m,
                LastUpdated = now
            })
            .ToList();

        _dbContext.Leaderboards.AddRange(entries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var usernames = await _dbContext.Players
            .AsNoTracking()
            .Where(player => entries.Select(entry => entry.PlayerId).Contains(player.Id))
            .ToDictionaryAsync(player => player.Id, player => player.Username, cancellationToken);

        return entries.Select(entry => new LeaderboardEntryDto
        {
            PlayerId = entry.PlayerId,
            Username = usernames.TryGetValue(entry.PlayerId, out var username) ? username : "Unknown",
            LeaderboardType = entry.LeaderboardType,
            Rank = entry.Rank,
            Score = entry.Score,
            PreviousScore = entry.PreviousScore,
            LastUpdated = entry.LastUpdated
        }).ToList();
    }

    private async Task<IReadOnlyList<(Guid PlayerId, decimal Score)>> LoadScoresAsync(string leaderboardType, CancellationToken cancellationToken)
    {
        return leaderboardType switch
        {
            "wealth" => await _dbContext.Players
                .AsNoTracking()
                .Select(player => new ValueTuple<Guid, decimal>(player.Id, player.NetWorth + player.LiquidCredits))
                .ToListAsync(cancellationToken),
            "reputation" => await _dbContext.Players
                .AsNoTracking()
                .Select(player => new ValueTuple<Guid, decimal>(player.Id, player.ReputationScore))
                .ToListAsync(cancellationToken),
            "combat" => await _dbContext.Players
                .AsNoTracking()
                .Select(player => new ValueTuple<Guid, decimal>(player.Id, player.FleetStrengthRating + (decimal)_dbContext.CombatLogs.Count(log => log.AttackerId == player.Id) * 25m))
                .ToListAsync(cancellationToken),
            "trade" => await _dbContext.Players
                .AsNoTracking()
                .Select(player => new ValueTuple<Guid, decimal>(
                    player.Id,
                    _dbContext.TradeTransactions
                        .Where(transaction => transaction.PlayerId == player.Id)
                        .Select(transaction => (decimal?)transaction.TotalPrice)
                        .Sum() ?? 0m))
                .ToListAsync(cancellationToken),
            _ => []
        };
    }

    private static string NormalizeType(string leaderboardType)
    {
        var normalizedType = leaderboardType?.Trim().ToLowerInvariant() ?? "wealth";
        if (!SupportedTypes.Contains(normalizedType))
        {
            throw new InvalidOperationException($"Unsupported leaderboard type '{leaderboardType}'.");
        }

        return normalizedType;
    }

    private static LeaderboardEntryDto MapEntry(LeaderboardEntity entry)
    {
        return new LeaderboardEntryDto
        {
            PlayerId = entry.PlayerId,
            Username = entry.Player?.Username ?? "Unknown",
            LeaderboardType = entry.LeaderboardType,
            Rank = entry.Rank,
            Score = entry.Score,
            PreviousScore = entry.PreviousScore,
            LastUpdated = entry.LastUpdated
        };
    }
}
