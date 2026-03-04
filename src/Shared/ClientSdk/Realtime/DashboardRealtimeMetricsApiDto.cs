namespace GalacticTrader.Desktop.Realtime;

public sealed class DashboardRealtimeMetricsApiDto
{
    public decimal LiquidCredits { get; init; }
    public decimal NetWorth { get; init; }
    public int ReputationScore { get; init; }
    public int FleetStrength { get; init; }
    public string ProtectionStatus { get; init; } = "Unknown";
    public int ActiveRoutes { get; init; }
    public int AlertCount { get; init; }
    public decimal GlobalEconomicIndex { get; init; }
}
