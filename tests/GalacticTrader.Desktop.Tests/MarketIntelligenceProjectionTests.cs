using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class MarketIntelligenceProjectionTests
{
    [Fact]
    public void Build_ProjectsAndSortsMarketIntelligenceData()
    {
        var summary = new MarketIntelligenceSummaryApiDto
        {
            VolatilityIndex = 42.84m,
            RegionalHeatmap =
            [
                new MarketHeatmapPointApiDto { SectorName = "Draco", TradeVolume = 1200m, TradeCount = 8 },
                new MarketHeatmapPointApiDto { SectorName = "Orion", TradeVolume = 3400m, TradeCount = 14 }
            ],
            TopTraders =
            [
                new TopTraderInsightApiDto { Username = "vex", TradeVolume = 2200m, TradeCount = 9 },
                new TopTraderInsightApiDto { Username = "nova", TradeVolume = 4200m, TradeCount = 17 }
            ],
            SmugglingCorridors =
            [
                new SmugglingCorridorInsightApiDto { FromSectorName = "Orion", ToSectorName = "Cygnus", SmugglingRuns = 5, AverageTradeValue = 410m },
                new SmugglingCorridorInsightApiDto { FromSectorName = "Draco", ToSectorName = "Lyra", SmugglingRuns = 8, AverageTradeValue = 380m }
            ]
        };

        var snapshot = MarketIntelligenceProjection.Build(summary);

        Assert.Equal(42.8m, snapshot.VolatilityIndex);
        Assert.Equal("Orion", snapshot.Heatmap[0].SectorName);
        Assert.Equal("nova", snapshot.TopTraders[0].Username);
        Assert.Equal("Draco -> Lyra", snapshot.SmugglingCorridors[0].Corridor);
        Assert.Equal(8, snapshot.SmugglingCorridors[0].SmugglingRuns);
        Assert.Equal("Draco -> Lyra", snapshot.TradeFlows[0].Flow);
        Assert.Equal("###########-", snapshot.TradeFlows[0].Diagram);
    }
}
