using System.Windows.Media;

namespace GalacticTrader.Desktop.Starmap;

public static class RouteVisualStyleResolver
{
    public static RouteVisualStyle Build(RouteSegment route)
    {
        var risk = Math.Clamp(route.BaseRiskScore, 0f, 100f);
        var riskRatio = risk / 100f;

        var low = Color.FromRgb(92, 168, 255);
        var high = Color.FromRgb(255, 94, 72);
        var color = Interpolate(low, high, riskRatio);
        var width = 0.5d + (riskRatio * 0.7d);
        var opacity = 0.72d + (riskRatio * 0.25d);

        return new RouteVisualStyle
        {
            Color = color,
            Width = Math.Round(width, 3),
            Opacity = Math.Round(Math.Clamp(opacity, 0.5d, 1d), 3)
        };
    }

    private static Color Interpolate(Color start, Color end, float ratio)
    {
        var r = (byte)Math.Round(start.R + ((end.R - start.R) * ratio));
        var g = (byte)Math.Round(start.G + ((end.G - start.G) * ratio));
        var b = (byte)Math.Round(start.B + ((end.B - start.B) * ratio));
        return Color.FromRgb(r, g, b);
    }
}
