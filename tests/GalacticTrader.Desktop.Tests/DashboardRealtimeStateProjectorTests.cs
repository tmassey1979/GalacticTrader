using GalacticTrader.ClientSdk.Dashboard;
using GalacticTrader.ClientSdk.Shell;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardRealtimeStateProjectorTests
{
    [Fact]
    public void ApplySnapshot_UpdatesMetricsRebuildsActionsAndMergesEvents()
    {
        var board = new DashboardActionBoard(
            new DashboardSnapshot(
                AvailableCredits: 1000m,
                ShipCount: 2,
                FleetStrength: 5,
                EscortStrength: 1,
                DangerousRouteCount: 0,
                ActiveIntelligenceCount: 0,
                ReputationScore: 10,
                EconomicStabilityIndex: 80m,
                ActivePlayers24h: 5),
            DashboardActionPlanner.Build(new DashboardSnapshot(
                AvailableCredits: 1000m,
                ShipCount: 2,
                FleetStrength: 5,
                EscortStrength: 1,
                DangerousRouteCount: 0,
                ActiveIntelligenceCount: 0,
                ReputationScore: 10,
                EconomicStabilityIndex: 80m,
                ActivePlayers24h: 5)));

        var existingFeed = new[]
        {
            new DashboardEventFeedEntry(
                new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc),
                "Trade",
                "Buy",
                "Cargo loaded")
        };

        var snapshot = new DashboardRealtimeSnapshotApiDto
        {
            CapturedAtUtc = new DateTime(2026, 3, 4, 10, 5, 0, DateTimeKind.Utc),
            Metrics = new DashboardRealtimeMetricsApiDto
            {
                LiquidCredits = 22000m,
                ReputationScore = -5,
                FleetStrength = 2,
                AlertCount = 4,
                GlobalEconomicIndex = 35m
            },
            Events =
            [
                new DashboardRealtimeEventApiDto
                {
                    OccurredAtUtc = new DateTime(2026, 3, 4, 10, 4, 0, DateTimeKind.Utc),
                    Category = "Intel",
                    Title = "Threat",
                    Detail = "pirate net active"
                }
            ]
        };

        var projected = DashboardRealtimeStateProjector.ApplySnapshot(board, existingFeed, snapshot);

        Assert.Equal(22000m, projected.Board.Snapshot.AvailableCredits);
        Assert.Equal(-5, projected.Board.Snapshot.ReputationScore);
        Assert.Equal(2, projected.Board.Snapshot.FleetStrength);
        Assert.Equal(4, projected.Board.Snapshot.DangerousRouteCount);
        Assert.Equal(35m, projected.Board.Snapshot.EconomicStabilityIndex);
        Assert.Contains(projected.Board.Actions, action => action.ActionType == DashboardActionType.ImproveLiquidity);
        Assert.Contains(projected.Board.Actions, action => action.ActionType == DashboardActionType.RepairReputation);
        Assert.Equal(2, projected.EventFeed.Count);
        Assert.Equal("Intel", projected.EventFeed[0].Category);
    }

    [Fact]
    public void ApplySnapshot_DeduplicatesRealtimeEvents()
    {
        var now = new DateTime(2026, 3, 4, 8, 0, 0, DateTimeKind.Utc);
        var board = new DashboardActionBoard(
            new DashboardSnapshot(100m, 1, 1, 1, 0, 0, 0, 90m, 2),
            DashboardActionPlanner.Build(new DashboardSnapshot(100m, 1, 1, 1, 0, 0, 0, 90m, 2)));
        var existing = new[]
        {
            new DashboardEventFeedEntry(now, "Intel", "Ping", "A")
        };
        var snapshot = new DashboardRealtimeSnapshotApiDto
        {
            Metrics = new DashboardRealtimeMetricsApiDto
            {
                LiquidCredits = 100m,
                ReputationScore = 0,
                FleetStrength = 1,
                AlertCount = 0,
                GlobalEconomicIndex = 90m
            },
            Events =
            [
                new DashboardRealtimeEventApiDto
                {
                    OccurredAtUtc = now,
                    Category = "Intel",
                    Title = "Ping",
                    Detail = "A"
                }
            ]
        };

        var projected = DashboardRealtimeStateProjector.ApplySnapshot(board, existing, snapshot);

        Assert.Single(projected.EventFeed);
    }
}
