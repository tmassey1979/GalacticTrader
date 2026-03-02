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

public sealed class LeaderboardPositionDto
{
    public Guid PlayerId { get; init; }
    public string LeaderboardType { get; init; } = string.Empty;
    public long Rank { get; init; }
    public long TotalPlayers { get; init; }
    public decimal Score { get; init; }
}

public sealed class LeaderboardHistoryPointDto
{
    public DateTime SnapshotAt { get; init; }
    public long Rank { get; init; }
    public decimal Score { get; init; }
}
