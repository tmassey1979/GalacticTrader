using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Intel;
using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class StatusMetricAggregatorTests
{
    [Fact]
    public void Build_ComputesSnapshotFromDataSets()
    {
        var transactions = new[]
        {
            new TradeExecutionResultApiDto { RemainingPlayerCredits = 8425.5m }
        };

        var standings = new[]
        {
            new PlayerFactionStandingApiDto { ReputationScore = 45 },
            new PlayerFactionStandingApiDto { ReputationScore = 71 }
        };

        var escort = new EscortSummaryApiDto
        {
            FleetStrength = 128
        };

        var threats = new[]
        {
            new ThreatAlert { Source = "Intel", Headline = "A", Detail = "a", Severity = 80f },
            new ThreatAlert { Source = "Route", Headline = "B", Detail = "b", Severity = 70f }
        };

        var scene = new StarmapScene(
            [],
            [
                new RouteSegment("A-B", new Point3D(), new Point3D(), false),
                new RouteSegment("B-C", new Point3D(), new Point3D(), true)
            ],
            new Model3DGroup());

        var snapshot = StatusMetricAggregator.Build(transactions, standings, escort, threats, scene);

        Assert.Equal(8425.5m, snapshot.LiquidCredits);
        Assert.Equal(71, snapshot.ReputationScore);
        Assert.Equal(128, snapshot.FleetStrength);
        Assert.Equal(2, snapshot.ActiveRoutes);
        Assert.Equal(2, snapshot.AlertCount);
    }
}
