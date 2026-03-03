using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Fleet;

public static class FleetCrewSkillWeightingProjector
{
    public static FleetCrewSkillWeighting Build(ShipApiDto ship)
    {
        if (ship is null)
        {
            throw new ArgumentNullException(nameof(ship));
        }

        var crewLoad = ship.CrewSlots > 0
            ? Math.Clamp(ship.CrewCount / (decimal)ship.CrewSlots, 0m, 1m)
            : 1m;
        var systemComplexity = Math.Clamp((ship.Hardpoints + ship.Modules.Count) / 10m, 0m, 1m);
        var reactorLoad = Math.Clamp(ship.ReactorOutput / 320m, 0m, 1m);

        var score = Math.Round(
            ((crewLoad * 0.55m) + (systemComplexity * 0.25m) + (reactorLoad * 0.20m)) * 100m,
            1,
            MidpointRounding.AwayFromZero);

        return new FleetCrewSkillWeighting
        {
            Score = score,
            Band = score switch
            {
                >= 80m => "Elite",
                >= 60m => "Veteran",
                >= 40m => "Standard",
                _ => "Undertrained"
            }
        };
    }
}
