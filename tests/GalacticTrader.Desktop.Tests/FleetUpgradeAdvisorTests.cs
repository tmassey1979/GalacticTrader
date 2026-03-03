using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Fleet;

namespace GalacticTrader.Desktop.Tests;

public sealed class FleetUpgradeAdvisorTests
{
    [Fact]
    public void Build_DamagedHull_ReturnsHighPriorityReinforcement()
    {
        var recommendation = FleetUpgradeAdvisor.Build(new ShipApiDto
        {
            HullIntegrity = 60,
            MaxHullIntegrity = 120,
            Modules = [new ShipModuleApiDto { Tier = 4 }]
        });

        Assert.Equal("High", recommendation.Priority);
        Assert.Equal("Install hull reinforcement package", recommendation.Recommendation);
    }

    [Fact]
    public void Build_LowModuleTier_ReturnsMediumUpgradeRecommendation()
    {
        var recommendation = FleetUpgradeAdvisor.Build(new ShipApiDto
        {
            HullIntegrity = 200,
            MaxHullIntegrity = 200,
            CargoCapacity = 800,
            Modules =
            [
                new ShipModuleApiDto { Tier = 1 },
                new ShipModuleApiDto { Tier = 2 }
            ]
        });

        Assert.Equal("Medium", recommendation.Priority);
        Assert.Equal("Upgrade core modules to Tier 3+", recommendation.Recommendation);
    }

    [Fact]
    public void Build_HealthyShip_ReturnsLowPriorityRecommendation()
    {
        var recommendation = FleetUpgradeAdvisor.Build(new ShipApiDto
        {
            HullIntegrity = 300,
            MaxHullIntegrity = 300,
            CargoCapacity = 900,
            Modules =
            [
                new ShipModuleApiDto { Tier = 4 },
                new ShipModuleApiDto { Tier = 4 },
                new ShipModuleApiDto { Tier = 5 }
            ]
        });

        Assert.Equal("Low", recommendation.Priority);
        Assert.Equal("Loadout balanced; monitor for specialist upgrades", recommendation.Recommendation);
    }
}
