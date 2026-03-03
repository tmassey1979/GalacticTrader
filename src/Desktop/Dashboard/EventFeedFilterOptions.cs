namespace GalacticTrader.Desktop.Dashboard;

public sealed class EventFeedFilterOptions
{
    public string Category { get; init; } = "All";
    public string Keyword { get; init; } = string.Empty;
    public TimeSpan? MaxAge { get; init; }
}
