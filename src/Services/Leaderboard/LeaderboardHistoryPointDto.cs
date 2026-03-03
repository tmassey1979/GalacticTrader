namespace GalacticTrader.Services.Leaderboard;

public sealed class LeaderboardHistoryPointDto
{
    public DateTime SnapshotAt { get; init; }
    public long Rank { get; init; }
    public decimal Score { get; init; }
}
