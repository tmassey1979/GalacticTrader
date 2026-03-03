using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class AnalyticsLeaderboardSnapshotBuilder
{
    public static AnalyticsLeaderboardSnapshot Build(
        IReadOnlyList<LeaderboardEntryApiDto> wealth,
        IReadOnlyList<LeaderboardEntryApiDto> trade,
        IReadOnlyList<LeaderboardEntryApiDto> combat,
        IReadOnlyList<LeaderboardEntryApiDto> reputation)
    {
        return new AnalyticsLeaderboardSnapshot
        {
            WealthLeader = FormatLeader(wealth),
            TradeLeader = FormatLeader(trade),
            CombatLeader = FormatLeader(combat),
            ReputationLeader = FormatLeader(reputation)
        };
    }

    private static string FormatLeader(IReadOnlyList<LeaderboardEntryApiDto> entries)
    {
        if (entries.Count == 0)
        {
            return "n/a";
        }

        var top = entries.OrderBy(static entry => entry.Rank).First();
        if (string.IsNullOrWhiteSpace(top.Username))
        {
            return "n/a";
        }

        return $"{top.Username} ({top.Score:N1})";
    }
}
