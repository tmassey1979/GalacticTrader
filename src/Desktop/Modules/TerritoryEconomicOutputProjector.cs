using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class TerritoryEconomicOutputProjector
{
    public static decimal BuildPerSystem(
        TerritoryDominanceApiDto record,
        decimal taxRatePercent,
        decimal tradeIncentivePercent)
    {
        if (record.ControlledSectorCount <= 0)
        {
            return 0m;
        }

        var basePerSystem =
            850m +
            ((decimal)record.InfrastructureControlScore * 9m) +
            ((decimal)record.DominanceScore * 5m) -
            ((decimal)record.WarMomentumScore * 3m);

        var policyFactor = 1m - (taxRatePercent / 200m) + (tradeIncentivePercent / 150m);
        policyFactor = Math.Clamp(policyFactor, 0.5m, 1.5m);

        return decimal.Round(Math.Max(0m, basePerSystem * policyFactor), 2, MidpointRounding.AwayFromZero);
    }
}
