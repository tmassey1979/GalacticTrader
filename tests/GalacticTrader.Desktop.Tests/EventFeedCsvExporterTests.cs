using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Tests;

public sealed class EventFeedCsvExporterTests
{
    [Fact]
    public void BuildCsv_ProducesHeaderAndEscapedRows()
    {
        var entries = new[]
        {
            new EventFeedEntry
            {
                OccurredAtUtc = new DateTime(2026, 3, 3, 1, 0, 0, DateTimeKind.Utc),
                Category = "Trade",
                Title = "Buy \"Ore\"",
                Detail = "qty, 200"
            }
        };

        var csv = EventFeedCsvExporter.BuildCsv(entries);

        Assert.Contains("timestamp_utc,category,title,detail", csv, StringComparison.Ordinal);
        Assert.Contains("\"Buy \"\"Ore\"\"\"", csv, StringComparison.Ordinal);
        Assert.Contains("\"qty, 200\"", csv, StringComparison.Ordinal);
    }
}
