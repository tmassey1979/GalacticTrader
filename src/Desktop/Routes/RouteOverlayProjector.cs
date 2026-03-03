using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Routes;

public static class RouteOverlayProjector
{
    public static RouteOverlayProjection Build(RouteHopApiDto hop)
    {
        var piratePresence = Math.Clamp(
            hop.BaseRiskScore + (hop.BaseFuelCost * 0.18f),
            0f,
            100f);

        var economicDensity = Math.Clamp(
            100f
            - hop.BaseRiskScore
            + (hop.BaseFuelCost * 0.22f)
            - (hop.BaseTravelTimeSeconds / 18f),
            0f,
            100f);

        return new RouteOverlayProjection
        {
            EconomicDensity = (float)Math.Round(economicDensity, 1),
            PiratePresenceProbability = (float)Math.Round(piratePresence, 1)
        };
    }
}
