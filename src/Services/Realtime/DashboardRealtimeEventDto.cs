namespace GalacticTrader.Services.Realtime;

public sealed class DashboardRealtimeEventDto
{
    public DateTime OccurredAtUtc { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}
