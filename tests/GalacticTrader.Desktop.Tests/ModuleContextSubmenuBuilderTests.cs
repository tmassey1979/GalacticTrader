using GalacticTrader.Desktop.Navigation;

namespace GalacticTrader.Desktop.Tests;

public sealed class ModuleContextSubmenuBuilderTests
{
    [Theory]
    [InlineData("Dashboard", "Wealth Overview")]
    [InlineData("Trading", "Commodity Filters")]
    [InlineData("Reputation", "Impact Forecast")]
    [InlineData("Analytics", "Leaderboards")]
    public void Build_ReturnsContextualSubmenuItems(string module, string expectedItem)
    {
        var items = ModuleContextSubmenuBuilder.Build(module);

        Assert.Contains(expectedItem, items);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void Build_ReturnsFallbackItems_ForUnknownModule()
    {
        var items = ModuleContextSubmenuBuilder.Build("Unknown");

        Assert.Equal(["Module Overview", "Primary Controls", "Detail Insights"], items);
    }
}
