using System.Text;

namespace GalacticTrader.Desktop.Dashboard;

public static class EventFeedCsvExporter
{
    public static string BuildCsv(IReadOnlyList<EventFeedEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("timestamp_utc,category,title,detail");

        foreach (var entry in entries)
        {
            builder.Append(Escape(entry.OccurredAtUtc.ToString("u")));
            builder.Append(',');
            builder.Append(Escape(entry.Category));
            builder.Append(',');
            builder.Append(Escape(entry.Title));
            builder.Append(',');
            builder.Append(Escape(entry.Detail));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        var normalized = value.Replace("\"", "\"\"");
        return $"\"{normalized}\"";
    }
}
