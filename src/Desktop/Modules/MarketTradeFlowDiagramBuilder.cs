namespace GalacticTrader.Desktop.Modules;

public static class MarketTradeFlowDiagramBuilder
{
    public static string Build(decimal flowIndex, int width = 12)
    {
        var normalized = Math.Clamp(flowIndex, 0m, 100m);
        var clampedWidth = Math.Clamp(width, 6, 30);
        var fillCount = (int)Math.Round((double)(normalized / 100m) * clampedWidth, MidpointRounding.AwayFromZero);
        fillCount = Math.Clamp(fillCount, 0, clampedWidth);
        return $"{new string('#', fillCount)}{new string('-', clampedWidth - fillCount)}";
    }
}
