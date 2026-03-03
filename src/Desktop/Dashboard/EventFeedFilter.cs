namespace GalacticTrader.Desktop.Dashboard;

public static class EventFeedFilter
{
    public static IReadOnlyList<EventFeedEntry> Apply(
        IReadOnlyList<EventFeedEntry> entries,
        EventFeedFilterOptions options,
        DateTime nowUtc)
    {
        var keyword = options.Keyword.Trim();

        var filtered = entries.Where(entry =>
        {
            if (!string.Equals(options.Category, "All", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(entry.Category, options.Category, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var foundInTitle = entry.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                var foundInDetail = entry.Detail.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                if (!foundInTitle && !foundInDetail)
                {
                    return false;
                }
            }

            if (options.MaxAge.HasValue)
            {
                var cutoff = nowUtc - options.MaxAge.Value;
                if (entry.OccurredAtUtc < cutoff)
                {
                    return false;
                }
            }

            return true;
        });

        return filtered.ToArray();
    }
}
