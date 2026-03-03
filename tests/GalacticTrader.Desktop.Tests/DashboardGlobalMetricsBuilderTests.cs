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
        Assert.Equal("vex (99.0)", snapshot.TopReputationPlayerDisplay);
        Assert.Equal("nova (12,450.0)", snapshot.TopFinancialPlayerDisplay);
    }
}
