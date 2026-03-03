namespace GalacticTrader.Desktop.Starmap;

public static class StarmapRouteTelemetryFormatter
{
    public static string Build(RouteSegment route)
    {
        return $"{route.Name} | Risk {route.BaseRiskScore:N1} | Density {route.EconomicDensity:N1} | Pirate {route.PiratePresenceProbability:N1}%";
    }
}
