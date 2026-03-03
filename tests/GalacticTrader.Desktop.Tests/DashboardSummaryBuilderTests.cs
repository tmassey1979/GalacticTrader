using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardSummaryBuilderTests
{
    [Fact]
    public void Build_ComputesStrategicSummary()
    {
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { RemainingPlayerCredits = 9500m, TotalPrice = 300m },
            new TradeExecutionResultApiDto { RemainingPlayerCredits = 9400m, TotalPrice = 200m }
        };

        var ships = new[]
        {
            new ShipApiDto { Name = "A", CurrentValue = 1500m },
            new ShipApiDto { Name = "B", CurrentValue = 500m }
        };

        var escort = new EscortSummaryApiDto { FleetStrength = 140 };
        var standings = new[]
        {
            new PlayerFactionStandingApiDto { ReputationScore = 40, HasAccess = true },
            new PlayerFactionStandingApiDto { ReputationScore = 65, HasAccess = false },
            new PlayerFactionStandingApiDto { ReputationScore = 51, HasAccess = true }
        };

        var dangerousRoutes = new[]
        {
            new RouteApiDto { FromSectorName = "A", ToSectorName = "B", BaseRiskScore = 85f }
        };

        var reports = new[]
        {
            new IntelligenceReportApiDto { IsExpired = false, ConfidenceScore = 0.8f, SignalType = "X", SectorName = "S" },
            new IntelligenceReportApiDto { IsExpired = true, ConfidenceScore = 0.9f, SignalType = "Y", SectorName = "T" }
        };

        var scene = new StarmapScene(
            [],
            [
                new RouteSegment("A-B", new Point3D(), new Point3D(), true),
                new RouteSegment("B-C", new Point3D(), new Point3D(), false)
            ],
            new Model3DGroup());

        var summary = DashboardSummaryBuilder.Build(
            transactions,
            ships,
            escort,
            standings,
            dangerousRoutes,
            reports,
            scene);

        Assert.Equal(9500m, summary.LiquidCredits);
        Assert.Equal(11500m, summary.NetWorth);
        Assert.Equal(82.6m, summary.AssetLiquidityRatio);
        Assert.Equal(5.3m, summary.CashFlowIndex);
        Assert.Equal(500m, summary.RecentTradeVolume);
        Assert.Equal([9400m, 9500m], summary.CashFlowTrend);
        Assert.Equal(2, summary.ShipCount);
        Assert.Equal(140, summary.FleetStrength);
        Assert.Equal(50m, summary.FleetRiskExposure);
        Assert.Equal(65, summary.HighestReputation);
        Assert.Equal(2, summary.AccessibleFactions);
        Assert.Equal(74.2m, summary.TradeReliabilityScore);
        Assert.Equal(75m, summary.ReputationInfluenceIndex);
        Assert.Equal(2, summary.TotalRoutes);
        Assert.Equal(1, summary.HighRiskRoutes);
        Assert.Equal(250m, summary.RevenuePerRoute);
        Assert.Equal(54m, summary.InterferenceProbability);
        Assert.Equal(2, summary.ThreatAlerts);
        Assert.Equal(1, summary.IntelligenceReports);
    }
}
