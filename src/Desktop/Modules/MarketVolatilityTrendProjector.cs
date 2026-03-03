namespace GalacticTrader.Desktop.Modules;

public static class MarketVolatilityTrendProjector
{
    public static MarketVolatilityTrendProjection Build(decimal volatilityIndex)
    {
        var rounded = decimal.Round(volatilityIndex, 1, MidpointRounding.AwayFromZero);
        var direction = rounded switch
        {
            >= 70m => "Rising",
            <= 30m => "Cooling",
            _ => "Stable"
        };
        var band = rounded switch
        {
            < 25m => "Low",
            < 50m => "Moderate",
            < 75m => "High",
            _ => "Extreme"
        };

        return new MarketVolatilityTrendProjection
        {
            Direction = direction,
            Band = band,
            Summary = $"{direction} ({band})"
        };
    }
}
