using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardAssetAllocationProjectorTests
{
    [Fact]
    public void Build_ProjectsLiquidAndShipClassSlices()
    {
        var ships = new[]
        {
            new ShipApiDto { ShipClass = "Frigate", CurrentValue = 2000m },
            new ShipApiDto { ShipClass = "Freighter", CurrentValue = 1000m },
            new ShipApiDto { ShipClass = "Frigate", CurrentValue = 500m }
        };

        var slices = DashboardAssetAllocationProjector.Build(5000m, ships, maxSlices: 5);

        Assert.Equal(3, slices.Count);
        Assert.Equal("Liquid Credits", slices[0].Label);
        Assert.Equal(5000m, slices[0].Value);
        Assert.Equal(58.8m, slices[0].Percent);

        Assert.Equal("Frigate Hulls", slices[1].Label);
        Assert.Equal(2500m, slices[1].Value);
        Assert.Equal(29.4m, slices[1].Percent);

        Assert.Equal("Freighter Hulls", slices[2].Label);
        Assert.Equal(1000m, slices[2].Value);
        Assert.Equal(11.8m, slices[2].Percent);
    }

    [Fact]
    public void Build_CollapsesExcessSlicesIntoOtherHoldings()
    {
        var ships = new[]
        {
            new ShipApiDto { ShipClass = "A", CurrentValue = 500m },
            new ShipApiDto { ShipClass = "B", CurrentValue = 400m },
            new ShipApiDto { ShipClass = "C", CurrentValue = 300m },
            new ShipApiDto { ShipClass = "D", CurrentValue = 200m }
        };

        var slices = DashboardAssetAllocationProjector.Build(0m, ships, maxSlices: 3);

        Assert.Equal(3, slices.Count);
        Assert.Equal("A Hulls", slices[0].Label);
        Assert.Equal(500m, slices[0].Value);
        Assert.Equal("B Hulls", slices[1].Label);
        Assert.Equal(400m, slices[1].Value);
        Assert.Equal("Other Holdings", slices[2].Label);
        Assert.Equal(500m, slices[2].Value);
    }

    [Fact]
    public void Build_ReturnsNoAssetsFallbackWhenTotalIsZero()
    {
        var slices = DashboardAssetAllocationProjector.Build(0m, [], maxSlices: 4);

        Assert.Single(slices);
        Assert.Equal("No assets", slices[0].Label);
        Assert.Equal(0m, slices[0].Percent);
        Assert.Equal("○", slices[0].PieGlyph);
    }
}
