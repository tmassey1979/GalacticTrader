using GalacticTrader.Desktop.Fleet;

namespace GalacticTrader.Desktop.Tests;

public sealed class FleetCrewUtilizationProjectorTests
{
    [Fact]
    public void Build_ReturnsUnderstaffed_WhenRatioBelowSixtyPercent()
    {
        var result = FleetCrewUtilizationProjector.Build(crewCount: 2, crewSlots: 5);

        Assert.Equal("Understaffed", result.Status);
        Assert.Equal(0.4f, result.Ratio);
    }

    [Fact]
    public void Build_ReturnsNominal_WhenWithinCapacity()
    {
        var result = FleetCrewUtilizationProjector.Build(crewCount: 6, crewSlots: 8);

        Assert.Equal("Nominal", result.Status);
        Assert.Equal(0.75f, result.Ratio);
    }

    [Fact]
    public void Build_ReturnsOverstaffed_WhenCrewExceedsSlots()
    {
        var result = FleetCrewUtilizationProjector.Build(crewCount: 7, crewSlots: 4);

        Assert.Equal("Overstaffed", result.Status);
        Assert.Equal(1.75f, result.Ratio);
    }
}
