namespace GalacticTrader.ClientSdk.Dashboard;

public sealed class DashboardEventFeedFilterOptions
{
    public string Category { get; init; } = "All";

    public string Keyword { get; init; } = string.Empty;

    public TimeSpan? MaxAge { get; init; }
}
