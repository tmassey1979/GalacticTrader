using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Intel;

namespace GalacticTrader.Desktop.Tests;

public sealed class ThreatAlertRankerTests
{
    [Fact]
    public void Build_RanksAlertsBySeverity_AndSkipsExpiredReports()
    {
        var routes = new[]
        {
            new RouteApiDto { FromSectorName = "A", ToSectorName = "B", BaseRiskScore = 72f },
            new RouteApiDto { FromSectorName = "B", ToSectorName = "C", BaseRiskScore = 83f }
        };

        var reports = new[]
        {
            new IntelligenceReportApiDto { SignalType = "PirateFlux", SectorName = "Kappa", Payload = "raiders massing", ConfidenceScore = 0.91f, IsExpired = false },
            new IntelligenceReportApiDto { SignalType = "OldSignal", SectorName = "Gamma", Payload = "stale", ConfidenceScore = 0.99f, IsExpired = true }
        };

        var ranked = ThreatAlertRanker.Build(routes, reports, maxItems: 5);

        Assert.Equal(3, ranked.Count);
        Assert.Equal("Intel", ranked[0].Source);
        Assert.Equal("Route", ranked[1].Source);
        Assert.DoesNotContain(ranked, static alert => alert.Headline.Contains("OldSignal", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_RespectsMaxItems()
    {
        var routes = Enumerable.Range(1, 6)
            .Select(index => new RouteApiDto
            {
                FromSectorName = $"S{index}",
                ToSectorName = $"T{index}",
                BaseRiskScore = 60f + index
            })
            .ToArray();

        var ranked = ThreatAlertRanker.Build(routes, [], maxItems: 3);

        Assert.Equal(3, ranked.Count);
        Assert.True(ranked[0].Severity >= ranked[1].Severity);
    }
}
