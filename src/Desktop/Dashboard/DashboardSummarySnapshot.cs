namespace GalacticTrader.Desktop.Dashboard;

public sealed class DashboardSummarySnapshot
{
    public decimal LiquidCredits { get; init; }
    public decimal NetWorth { get; init; }
    public decimal AssetLiquidityRatio { get; init; }
    public decimal CashFlowIndex { get; init; }
    public decimal RecentTradeVolume { get; init; }
    public IReadOnlyList<decimal> CashFlowTrend { get; init; } = [];
    public int ShipCount { get; init; }
    public int FleetStrength { get; init; }
    public decimal FleetRiskExposure { get; init; }
    public int HighestReputation { get; init; }
    public int AccessibleFactions { get; init; }
    public decimal ReputationInfluenceIndex { get; init; }
    public int TotalRoutes { get; init; }
    public int HighRiskRoutes { get; init; }
    public decimal RevenuePerRoute { get; init; }
    public decimal InterferenceProbability { get; init; }
    public int ThreatAlerts { get; init; }
    public int IntelligenceReports { get; init; }
}
