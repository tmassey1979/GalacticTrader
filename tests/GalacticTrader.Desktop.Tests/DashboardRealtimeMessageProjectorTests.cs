using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardRealtimeMessageProjectorTests
{
    [Fact]
    public void ApplySnapshot_MapsMetricsAndMergesEvents()
    {
        var now = new DateTime(2026, 3, 3, 2, 0, 0, DateTimeKind.Utc);
        var existing = new[]
        {
            new EventFeedEntry
            {
                OccurredAtUtc = now.AddMinutes(-8),
                Category = "Trade",
                Title = "Buy x5",
                Detail = "Tariff 1 | Total 90 | Success"
            }
        };
        var snapshot = new DashboardRealtimeSnapshotApiDto
        {
            CapturedAtUtc = now,
            Metrics = new DashboardRealtimeMetricsApiDto
            {
                LiquidCredits = 4200m,
                ReputationScore = 77,
                FleetStrength = 44,
                ActiveRoutes = 12,
                AlertCount = 5
            },
            Events =
            [
                new DashboardRealtimeEventApiDto
                {
                    OccurredAtUtc = now.AddMinutes(-8),
                    Category = "Trade",
                    Title = "Buy x5",
                    Detail = "Tariff 1 | Total 90 | Success"
                },
                new DashboardRealtimeEventApiDto
                {
                    OccurredAtUtc = now.AddMinutes(-2),
                    Category = "Intel",
                    Title = "Jammer @ Orion",
                    Detail = "Convoy lane spotted"
                }
            ]
        };

        var projection = DashboardRealtimeMessageProjector.ApplySnapshot(existing, snapshot);

        Assert.Equal(4200m, projection.Metrics.LiquidCredits);
        Assert.Equal(77, projection.Metrics.ReputationScore);
        Assert.Equal(44, projection.Metrics.FleetStrength);
        Assert.Equal(12, projection.Metrics.ActiveRoutes);
        Assert.Equal(5, projection.Metrics.AlertCount);

        Assert.Equal(2, projection.Events.Count);
        Assert.Equal("Intel", projection.Events[0].Category);
        Assert.Equal("Trade", projection.Events[1].Category);
    }
}
