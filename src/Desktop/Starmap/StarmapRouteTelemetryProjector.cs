namespace GalacticTrader.Desktop.Starmap;

public static class StarmapRouteTelemetryProjector
{
    public static StarmapRouteTelemetryProjection Build(float baseRiskScore)
    {
        var clampedRisk = Math.Clamp(baseRiskScore, 0f, 100f);
        var economicDensity = Math.Clamp(95f - (clampedRisk * 0.62f), 8f, 95f);
        var piratePresence = Math.Clamp(6f + (clampedRisk * 0.88f), 4f, 95f);

        return new StarmapRouteTelemetryProjection
        {
            BaseRiskScore = (float)Math.Round(clampedRisk, 1, MidpointRounding.AwayFromZero),
            EconomicDensity = (float)Math.Round(economicDensity, 1, MidpointRounding.AwayFromZero),
            PiratePresenceProbability = (float)Math.Round(piratePresence, 1, MidpointRounding.AwayFromZero)
        };
    }
}
