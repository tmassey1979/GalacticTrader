namespace GalacticTrader.Services.Telemetry;

public sealed class GlobalMetricsSummaryDto
{
    public int TotalUsers { get; init; }
    public int ActivePlayers24h { get; init; }
    public decimal AvgBattlesPerHour { get; init; }
    public decimal EconomicStabilityIndex { get; init; }
    public GlobalTopPlayerDto TopReputationPlayer { get; init; } = new();
    public GlobalTopPlayerDto TopFinancialPlayer { get; init; } = new();
}
