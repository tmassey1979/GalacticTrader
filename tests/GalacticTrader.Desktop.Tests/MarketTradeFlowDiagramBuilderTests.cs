using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class MarketTradeFlowDiagramBuilderTests
{
    [Fact]
    public void Build_ClampsLowAndHighBounds()
    {
        var low = MarketTradeFlowDiagramBuilder.Build(-10m, width: 10);
        var high = MarketTradeFlowDiagramBuilder.Build(140m, width: 10);

        Assert.Equal("----------", low);
        Assert.Equal("##########", high);
    }

    [Fact]
    public void Build_UsesConfiguredWidth()
    {
        var value = MarketTradeFlowDiagramBuilder.Build(50m, width: 8);

        Assert.Equal(8, value.Length);
        Assert.Equal("####----", value);
    }
}
