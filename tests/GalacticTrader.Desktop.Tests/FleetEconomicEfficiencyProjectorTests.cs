using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Fleet;

namespace GalacticTrader.Desktop.Tests;

public sealed class FleetEconomicEfficiencyProjectorTests
{
    [Fact]
    public void Build_AssignedRouteScoresHigherThanUnassigned()
    {
        var baseline = new ShipApiDto
        {
            HullIntegrity = 180,
            MaxHullIntegrity = 200,
            CargoCapacity = 900,
            ReactorOutput = 260,
            CrewCount = 10,
            CrewSlots = 12,
            AssignedRoute = "Unassigned",
            Modules =
            [
                new ShipModuleApiDto { Tier = 2 },
                new ShipModuleApiDto { Tier = 3 },
                new ShipModuleApiDto { Tier = 4 }
            ]
        };

        var assigned = new ShipApiDto
        {
            HullIntegrity = baseline.HullIntegrity,
            MaxHullIntegrity = baseline.MaxHullIntegrity,
            CargoCapacity = baseline.CargoCapacity,
            ReactorOutput = baseline.ReactorOutput,
            CrewCount = baseline.CrewCount,
            CrewSlots = baseline.CrewSlots,
            AssignedRoute = "Sirius-Helios Corridor",
            Modules = baseline.Modules
        };

        var unassignedScore = FleetEconomicEfficiencyProjector.Build(baseline);
        var assignedScore = FleetEconomicEfficiencyProjector.Build(assigned);

        Assert.True(assignedScore > unassignedScore);
    }

    [Fact]
    public void Build_ClampsScoreWithinZeroToHundred()
    {
        var damagedShip = new ShipApiDto
        {
            HullIntegrity = 0,
            MaxHullIntegrity = 250,
            CargoCapacity = 0,
            ReactorOutput = 0,
            CrewCount = 0,
            CrewSlots = 8,
            AssignedRoute = "Unassigned"
        };

        var topShip = new ShipApiDto
        {
            HullIntegrity = 1000,
            MaxHullIntegrity = 1000,
            CargoCapacity = 3000,
            ReactorOutput = 800,
            CrewCount = 40,
            CrewSlots = 40,
            AssignedRoute = "Alpha-Beta",
            Modules = Enumerable.Range(0, 12).Select(_ => new ShipModuleApiDto { Tier = 6 }).ToArray()
        };

        var damagedScore = FleetEconomicEfficiencyProjector.Build(damagedShip);
        var topScore = FleetEconomicEfficiencyProjector.Build(topShip);

        Assert.InRange(damagedScore, 0m, 100m);
        Assert.InRange(topScore, 0m, 100m);
    }
}
