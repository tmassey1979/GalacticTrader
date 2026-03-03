namespace GalacticTrader.Desktop.Dashboard;

public static class TopStatusTooltipBuilder
{
    private static readonly char[] SparklinePalette = ['.', ':', '-', '=', '+', '*', '#'];

    public static string BuildNumeric(string label, IReadOnlyList<decimal> samples, string format)
    {
        if (samples.Count == 0)
        {
            return $"{label}\nNo trend data yet.";
        }

        var current = samples[^1];
        var delta = current - samples[0];
        var direction = delta switch
        {
            > 0m => "up",
            < 0m => "down",
            _ => "flat"
        };

        return $"{label}\nCurrent: {current.ToString(format)}\nTrend ({samples.Count} samples): {direction} ({delta:+0.##;-0.##;0})\nSeries: {BuildSparkline(samples)}";
    }

    public static string BuildLabelTrend(string label, IReadOnlyList<string> samples)
    {
        if (samples.Count == 0)
        {
            return $"{label}\nNo trend data yet.";
        }

        var changes = 0;
        for (var i = 1; i < samples.Count; i++)
        {
            if (!string.Equals(samples[i], samples[i - 1], StringComparison.OrdinalIgnoreCase))
            {
                changes++;
            }
        }

        var stability = changes == 0 ? "stable" : "shifting";
        return $"{label}\nCurrent: {samples[^1]}\nState transitions: {changes}\nStability: {stability}\nHistory: {string.Join(" -> ", samples)}";
    }

    private static string BuildSparkline(IReadOnlyList<decimal> samples)
    {
        if (samples.Count == 0)
        {
            return string.Empty;
        }

        if (samples.Count == 1)
        {
            return ".";
        }

        var min = samples.Min();
        var max = samples.Max();
        if (min == max)
        {
            return new string('=', samples.Count);
        }

        var span = max - min;
        var result = new char[samples.Count];
        for (var i = 0; i < samples.Count; i++)
        {
            var normalized = (samples[i] - min) / span;
            var paletteIndex = (int)Math.Round(normalized * (SparklinePalette.Length - 1), MidpointRounding.AwayFromZero);
            result[i] = SparklinePalette[Math.Clamp(paletteIndex, 0, SparklinePalette.Length - 1)];
        }

        return new string(result);
    }
}
