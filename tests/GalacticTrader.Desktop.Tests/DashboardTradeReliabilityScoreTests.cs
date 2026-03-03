using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardTradeReliabilityScoreTests
{
    [Fact]
    public void Build_ClampsTradeReliabilityToHundredForStrongStandingProfile()
    {
        var standings = Enumerable.Range(0, 6)
            .Select(_ => new PlayerFactionStandingApiDto
            {
                ReputationScore = 100,
                HasAccess = true,
                TradingDiscount = 0.20m
            })
            .ToArray();

        var summary = DashboardSummaryBuilder.Build(
            transactions: [],
            ships: [],
            escortSummary: null,
            standings: standings,
            dangerousRoutes: [],
            reports: [],
            scene: new StarmapScene([], [], new Model3DGroup()));

        Assert.Equal(100m, summary.TradeReliabilityScore);
    }

    [Fact]
    public void Build_ClampsTradeReliabilityToZeroWhenRiskOverwhelmsStanding()
    {
        var scene = new StarmapScene(
            [],
            Enumerable.Range(0, 20)
                .Select(index => new RouteSegment(
                    $"R{index}",
                    new Point3D(index, 0, 0),
                    new Point3D(index + 1, 0, 0),
                    true))
                .ToArray(),
            new Model3DGroup());

        var standings = Enumerable.Range(0, 6)
            .Select(_ => new PlayerFactionStandingApiDto
            {
                ReputationScore = -100,
                HasAccess = false,
                TradingDiscount = 0m
            })
            .ToArray();

        var summary = DashboardSummaryBuilder.Build(
            transactions: [],
            ships: [],
            escortSummary: null,
            standings: standings,
            dangerousRoutes: [],
            reports: [],
            scene: scene);

        Assert.Equal(0m, summary.TradeReliabilityScore);
    }
}
