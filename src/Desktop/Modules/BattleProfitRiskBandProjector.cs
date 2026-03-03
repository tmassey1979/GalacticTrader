namespace GalacticTrader.Desktop.Modules;

public static class BattleProfitRiskBandProjector
{
    public static string Build(decimal battleToProfitRatio)
    {
        return battleToProfitRatio switch
        {
            <= 15m => "Efficient",
            <= 45m => "Balanced",
            <= 80m => "Exposed",
            _ => "Critical Exposure"
        };
    }
}
