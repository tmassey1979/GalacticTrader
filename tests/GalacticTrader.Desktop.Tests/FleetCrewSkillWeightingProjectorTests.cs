using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Fleet;

namespace GalacticTrader.Desktop.Tests;

public sealed class FleetCrewSkillWeightingProjectorTests
{
    [Fact]
    public void Build_HighReadinessShip_ReturnsEliteBand()
    {
        var ship = new ShipApiDto
        {
            CrewCount = 24,
            CrewSlots = 24,
            Hardpoints = 5,
            ReactorOutput = 360,
            Modules = Enumerable.Range(0, 5).Select(_ => new ShipModuleApiDto { Tier = 4 }).ToArray()
        };

        var weighting = FleetCrewSkillWeightingProjector.Build(ship);

        Assert.InRange(weighting.Score, 80m, 100m);
        Assert.Equal("Elite", weighting.Band);
    }

    [Fact]
    public void Build_LowReadinessShip_ReturnsUndertrainedBand()
    {
        var ship = new ShipApiDto
        {
            CrewCount = 1,
            CrewSlots = 12,
            Hardpoints = 1,
            ReactorOutput = 80,
            Modules = []
        };

        var weighting = FleetCrewSkillWeightingProjector.Build(ship);

        Assert.InRange(weighting.Score, 0m, 39.9m);
        Assert.Equal("Undertrained", weighting.Band);
    }
}
