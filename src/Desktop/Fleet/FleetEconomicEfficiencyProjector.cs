using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Fleet;

public static class FleetEconomicEfficiencyProjector
{
    public static decimal Build(ShipApiDto ship)
    {
        if (ship is null)
        {
            throw new ArgumentNullException(nameof(ship));
        }

        var hullHealth = ship.MaxHullIntegrity > 0
            ? Math.Clamp(ship.HullIntegrity / (decimal)ship.MaxHullIntegrity, 0m, 1m)
            : 0m;

        var crewUtilization = ship.CrewSlots > 0
            ? Math.Clamp(ship.CrewCount / (decimal)ship.CrewSlots, 0m, 1m)
            : 1m;

        var cargoFactor = Math.Clamp(ship.CargoCapacity / 700m, 0.35m, 1.85m);
        var reactorFactor = Math.Clamp(ship.ReactorOutput / 220m, 0.45m, 1.65m);
        var moduleTierAverage = ship.Modules.Count > 0
            ? ship.Modules.Average(static module => module.Tier)
            : 0d;
        var moduleFactor = Math.Clamp((decimal)moduleTierAverage / 4m, 0m, 1.25m);

        var assignmentFactor = string.IsNullOrWhiteSpace(ship.AssignedRoute) ||
            ship.AssignedRoute.Equals("Unassigned", StringComparison.OrdinalIgnoreCase)
            ? 0.88m
            : 1.02m;

        var blendedScore =
            (hullHealth * 0.30m) +
            (crewUtilization * 0.25m) +
            (cargoFactor * 0.20m) +
            (reactorFactor * 0.15m) +
            (moduleFactor * 0.10m);

        var score = Math.Clamp(blendedScore * assignmentFactor * 100m, 0m, 100m);
        return Math.Round(score, 2, MidpointRounding.AwayFromZero);
    }
}
