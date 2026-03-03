using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Trading;

namespace GalacticTrader.Desktop.Tests;

public sealed class TradeHeatmapProjectorTests
{
    [Fact]
    public void Build_OrdersByVolumeAndRespectsMaxRows()
    {
        var summary = new MarketIntelligenceSummaryApiDto
        {
            RegionalHeatmap =
            [
                new MarketHeatmapPointApiDto { SectorName = "Gamma", TradeVolume = 80m, TradeCount = 9 },
                new MarketHeatmapPointApiDto { SectorName = "Alpha", TradeVolume = 120m, TradeCount = 12 },
                new MarketHeatmapPointApiDto { SectorName = "Beta", TradeVolume = 90m, TradeCount = 10 }
            ]
        };

        var rows = TradeHeatmapProjector.Build(summary, maxRows: 2);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Alpha", rows[0].SectorName);
        Assert.Equal("Beta", rows[1].SectorName);
    }
}
