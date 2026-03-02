namespace GalacticTrader.Services.Leaderboard;

public interface ILeaderboardService
{
    Task<IReadOnlyList<LeaderboardEntryDto>> RecalculateAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(string leaderboardType, int limit = 50, CancellationToken cancellationToken = default);
    Task<LeaderboardPositionDto?> GetPlayerPositionAsync(Guid playerId, string leaderboardType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaderboardHistoryPointDto>> GetHistoryAsync(Guid playerId, string leaderboardType, int limit = 20, CancellationToken cancellationToken = default);
    Task<int> ResetLeaderboardAsync(string leaderboardType, CancellationToken cancellationToken = default);
}
