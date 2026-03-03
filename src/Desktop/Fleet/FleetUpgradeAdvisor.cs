using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Fleet;

public static class FleetUpgradeAdvisor
{
    public static FleetUpgradeRecommendation Build(ShipApiDto ship)
    {
        if (ship is null)
        {
            throw new ArgumentNullException(nameof(ship));
        }

        var hullPercent = ship.MaxHullIntegrity > 0
            ? ship.HullIntegrity / (decimal)ship.MaxHullIntegrity * 100m
            : 0m;
        var moduleTierAverage = ship.Modules.Count > 0
            ? (decimal)ship.Modules.Average(static module => module.Tier)
            : 0m;

        if (hullPercent < 70m)
        {
            return new FleetUpgradeRecommendation
            {
                Priority = "High",
                Recommendation = "Install hull reinforcement package"
            };
        }

        if (ship.Modules.Count == 0)
        {
            return new FleetUpgradeRecommendation
            {
                Priority = "High",
                Recommendation = "Install baseline navigation and shield modules"
            };
        }

        if (moduleTierAverage < 3m)
        {
            return new FleetUpgradeRecommendation
            {
                Priority = "Medium",
                Recommendation = "Upgrade core modules to Tier 3+"
            };
        }

        if (ship.CargoCapacity < 450)
        {
            return new FleetUpgradeRecommendation
            {
                Priority = "Medium",
                Recommendation = "Expand cargo hold for route profitability"
            };
        }

        return new FleetUpgradeRecommendation
        {
            Priority = "Low",
            Recommendation = "Loadout balanced; monitor for specialist upgrades"
        };
    }
}
