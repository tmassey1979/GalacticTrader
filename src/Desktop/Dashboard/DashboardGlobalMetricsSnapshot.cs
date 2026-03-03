namespace GalacticTrader.Desktop.Dashboard;

public sealed class DashboardGlobalMetricsSnapshot
{
    public int TotalUsers { get; init; }
    public int ActivePlayers24h { get; init; }
    public decimal AvgBattlesPerHour { get; init; }
    public decimal EconomicStabilityIndex { get; init; }
    public decimal TradeVolumeIndex { get; init; }
    public decimal FactionStabilityIndex { get; init; }
    public decimal CombatIntensityIndex { get; init; }
    public string TopReputationPlayerDisplay { get; init; } = "n/a";
    public string TopFinancialPlayerDisplay { get; init; } = "n/a";
}
