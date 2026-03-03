namespace GalacticTrader.Desktop.Modules;

public static class ServicesArchetypeDistributionBuilder
{
    public static IReadOnlyList<ServicesArchetypeDistributionEntry> Build(IReadOnlyList<ServicesAgentDisplayRow> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var total = rows.Count;
        return rows
            .GroupBy(static row => Normalize(row.Archetype), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var count = group.Count();
                var sharePercent = Math.Round((count * 100d) / total, 1);
                return new ServicesArchetypeDistributionEntry
                {
                    Archetype = group.Key,
                    AgentCount = count,
                    SharePercent = sharePercent,
                    ShareSummary = $"{count} ({sharePercent:N1}%)"
                };
            })
            .OrderByDescending(static entry => entry.AgentCount)
            .ThenBy(static entry => entry.Archetype, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Normalize(string archetype)
    {
        return string.IsNullOrWhiteSpace(archetype)
            ? "Unknown"
            : archetype.Trim();
    }
}
