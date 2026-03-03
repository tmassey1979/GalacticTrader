using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardGlobalMetricsBuilderTests
{
    [Fact]
    public void Build_ProjectsGlobalSummaryToDashboardSnapshot()
    {
        var summary = new GlobalMetricsSummaryApiDto
        {
            TotalUsers = 128,
            ActivePlayers24h = 47,
            AvgBattlesPerHour = 2.345m,
            EconomicStabilityIndex = 88.76m,
            TopReputationPlayer = new GlobalTopPlayerApiDto
            {
                Username = "vex",
                Score = 99m
            },
            TopFinancialPlayer = new GlobalTopPlayerApiDto
            {
                Username = "nova",
                Score = 12450m
            }
        };

        var snapshot = DashboardGlobalMetricsBuilder.Build(summary);

        Assert.Equal(128, snapshot.TotalUsers);
        Assert.Equal(47, snapshot.ActivePlayers24h);
        Assert.Equal(2.35m, snapshot.AvgBattlesPerHour);
        Assert.Equal(88.8m, snapshot.EconomicStabilityIndex);
        Assert.Equal(68.9m, snapshot.TradeVolumeIndex);
        Assert.Equal(88.6m, snapshot.FactionStabilityIndex);
        Assert.Equal(28.2m, snapshot.CombatIntensityIndex);
        Assert.Equal("vex (99.0)", snapshot.TopReputationPlayerDisplay);
        Assert.Equal("nova (12,450.0)", snapshot.TopFinancialPlayerDisplay);
    }

    [Fact]
    public void Build_ClampsDerivedIndexesToZeroThroughHundred()
    {
        var highSummary = new GlobalMetricsSummaryApiDto
        {
            ActivePlayers24h = 9999,
            AvgBattlesPerHour = 50m,
            EconomicStabilityIndex = 999m
        };

        var lowStabilitySummary = new GlobalMetricsSummaryApiDto
        {
            ActivePlayers24h = 0,
            AvgBattlesPerHour = 50m,
            EconomicStabilityIndex = 0m
        };

        var highSnapshot = DashboardGlobalMetricsBuilder.Build(highSummary);
        var lowStabilitySnapshot = DashboardGlobalMetricsBuilder.Build(lowStabilitySummary);

        Assert.Equal(100m, highSnapshot.TradeVolumeIndex);
        Assert.Equal(100m, highSnapshot.CombatIntensityIndex);
        Assert.Equal(0m, lowStabilitySnapshot.FactionStabilityIndex);
    }
}
