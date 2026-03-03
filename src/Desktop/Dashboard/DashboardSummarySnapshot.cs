namespace GalacticTrader.Desktop.Dashboard;

public sealed class DashboardSummarySnapshot
{
    public decimal LiquidCredits { get; init; }
    public decimal RecentTradeVolume { get; init; }
    public int ShipCount { get; init; }
    public int FleetStrength { get; init; }
    public int HighestReputation { get; init; }
    public int AccessibleFactions { get; init; }
    public int TotalRoutes { get; init; }
    public int HighRiskRoutes { get; init; }
    public int ThreatAlerts { get; init; }
    public int IntelligenceReports { get; init; }
}
