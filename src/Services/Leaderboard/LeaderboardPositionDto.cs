namespace GalacticTrader.Services.Leaderboard;

public sealed class LeaderboardPositionDto
{
    public Guid PlayerId { get; init; }
    public string LeaderboardType { get; init; } = string.Empty;
    public long Rank { get; init; }
    public long TotalPlayers { get; init; }
    public decimal Score { get; init; }
}
