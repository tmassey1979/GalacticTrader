using GalacticTrader.ClientSdk.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardEventFeedUtilitiesTests
{
    [Fact]
    public void Filter_AppliesCategoryKeywordAndMaxAge()
    {
        var now = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        var entries = new[]
        {
            new DashboardEventFeedEntry(now.AddHours(-3), "Trade", "Buy Ore", "short haul"),
            new DashboardEventFeedEntry(now.AddMinutes(-30), "Trade", "Sell Ore", "pirate corridor"),
            new DashboardEventFeedEntry(now.AddMinutes(-10), "Intel", "Threat Alert", "pirate activity")
        };

        var options = new DashboardEventFeedFilterOptions
        {
            Category = "Trade",
            Keyword = "pirate",
            MaxAge = TimeSpan.FromHours(1)
        };

        var filtered = DashboardEventFeedFilter.Apply(entries, options, now);

        Assert.Single(filtered);
        Assert.Equal("Sell Ore", filtered[0].Title);
    }

    [Fact]
    public void CsvExporter_ProducesEscapedRows()
    {
        var entries = new[]
        {
            new DashboardEventFeedEntry(
                new DateTime(2026, 3, 4, 12, 15, 0, DateTimeKind.Utc),
                "Intel",
                "Signal \"Spike\"",
                "Detail with comma, and quote \"inside\"")
        };

        var csv = DashboardEventFeedCsvExporter.BuildCsv(entries);

        Assert.Contains("timestamp_utc,category,title,detail", csv);
        Assert.Contains("\"Signal \"\"Spike\"\"\"", csv);
        Assert.Contains("\"Detail with comma, and quote \"\"inside\"\"\"", csv);
    }
}
