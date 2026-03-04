namespace GalacticTrader.Desktop.Api;

public sealed class GlobalMetricsSummaryApiDto
{
    public int TotalUsers { get; init; }
    public int ActivePlayers24h { get; init; }
    public decimal AvgBattlesPerHour { get; init; }
    public decimal EconomicStabilityIndex { get; init; }
    public GlobalTopPlayerApiDto TopReputationPlayer { get; init; } = new();
    public GlobalTopPlayerApiDto TopFinancialPlayer { get; init; } = new();
}
