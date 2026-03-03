namespace GalacticTrader.Desktop.Modules;

public sealed class AnalyticsLeaderboardSnapshot
{
    public required string WealthLeader { get; init; }
    public required string TradeLeader { get; init; }
    public required string CombatLeader { get; init; }
    public required string ReputationLeader { get; init; }
}
