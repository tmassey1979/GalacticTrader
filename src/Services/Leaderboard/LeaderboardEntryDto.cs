namespace GalacticTrader.Services.Leaderboard;

public sealed class LeaderboardEntryDto
{
    public Guid PlayerId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string LeaderboardType { get; init; } = string.Empty;
    public long Rank { get; init; }
    public decimal Score { get; init; }
    public decimal PreviousScore { get; init; }
    public DateTime LastUpdated { get; init; }
}
