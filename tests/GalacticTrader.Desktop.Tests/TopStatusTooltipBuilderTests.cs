using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class TopStatusTooltipBuilderTests
{
    [Fact]
    public void BuildNumeric_FormatsTrendAndSeries()
    {
        var tooltip = TopStatusTooltipBuilder.BuildNumeric(
            "Liquid Credits",
            [100m, 140m, 120m, 190m],
            "N0");

        Assert.Contains("Liquid Credits", tooltip, StringComparison.Ordinal);
        Assert.Contains("Current: 190", tooltip, StringComparison.Ordinal);
        Assert.Contains("Trend (4 samples): up (+90)", tooltip, StringComparison.Ordinal);
        Assert.Contains("Min/Max: 100 / 190", tooltip, StringComparison.Ordinal);
        Assert.Contains("Graph:", tooltip, StringComparison.Ordinal);
        Assert.DoesNotContain("Series:", tooltip, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildLabelTrend_ReportsTransitionCount()
    {
        var tooltip = TopStatusTooltipBuilder.BuildLabelTrend(
            "Protection Status",
            ["Fragile", "Fragile", "Guarded", "Guarded", "Fortified"]);

        Assert.Contains("Current: Fortified", tooltip, StringComparison.Ordinal);
        Assert.Contains("State transitions: 2", tooltip, StringComparison.Ordinal);
        Assert.Contains("Stability: shifting", tooltip, StringComparison.Ordinal);
    }
}
