using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class AnalyticsTrendBuilderTests
{
    [Fact]
    public void BuildRevenueBars_NormalizesHeightsAndOrdersChronologically()
    {
        var trades = new[]
        {
            new TradeExecutionResultApiDto { TotalPrice = 100m },
            new TradeExecutionResultApiDto { TotalPrice = 150m },
            new TradeExecutionResultApiDto { TotalPrice = 50m }
        };

        var bars = AnalyticsTrendBuilder.BuildRevenueBars(trades, maxPoints: 3, maxHeight: 60d);

        Assert.Equal(3, bars.Count);
        Assert.Equal(50m, bars[0].Value);
        Assert.Equal(150m, bars[1].Value);
        Assert.True(bars[1].Height > bars[0].Height);
    }
}
