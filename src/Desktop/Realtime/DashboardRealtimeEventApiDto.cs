namespace GalacticTrader.Desktop.Realtime;

public sealed class DashboardRealtimeEventApiDto
{
    public DateTime OccurredAtUtc { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}
