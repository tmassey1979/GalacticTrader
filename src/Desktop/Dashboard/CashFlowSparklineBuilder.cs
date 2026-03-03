using System.Windows;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Dashboard;

public static class CashFlowSparklineBuilder
{
    public static PointCollection Build(IReadOnlyList<decimal> values, double width, double height)
    {
        var points = new PointCollection();
        if (values.Count == 0 || width <= 0 || height <= 0)
        {
            return points;
        }

        if (values.Count == 1)
        {
            points.Add(new Point(0, height / 2d));
            points.Add(new Point(width, height / 2d));
            return points;
        }

        var min = values.Min();
        var max = values.Max();
        var range = max - min;

        for (var index = 0; index < values.Count; index++)
        {
            var x = (width * index) / (values.Count - 1d);
            var ratio = range <= 0m ? 0.5d : (double)((values[index] - min) / range);
            var y = height - (ratio * height);
            points.Add(new Point(x, y));
        }

        return points;
    }
}
