namespace GalacticTrader.Desktop.Dashboard;

public sealed class EventFeedEntry
{
    public DateTime OccurredAtUtc { get; init; }
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Detail { get; init; }
}
