namespace GalacticTrader.Desktop.Modules;

public static class ServicesStrategyBiasDistributionBuilder
{
    public static IReadOnlyList<ServicesStrategyBiasDistributionEntry> Build(IReadOnlyList<ServicesAgentDisplayRow> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var total = rows.Count;
        return rows
            .GroupBy(static row => Normalize(row.StrategyBias), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var count = group.Count();
                var sharePercent = Math.Round((count * 100d) / total, 1);
                return new ServicesStrategyBiasDistributionEntry
                {
                    StrategyBias = group.Key,
                    AgentCount = count,
                    SharePercent = sharePercent,
                    ShareSummary = $"{count} ({sharePercent:N1}%)"
                };
            })
            .OrderByDescending(static entry => entry.AgentCount)
            .ThenBy(static entry => entry.StrategyBias, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Normalize(string strategyBias)
    {
        return string.IsNullOrWhiteSpace(strategyBias)
            ? "Unknown"
            : strategyBias.Trim();
    }
}
