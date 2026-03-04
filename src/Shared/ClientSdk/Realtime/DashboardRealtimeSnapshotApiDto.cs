namespace GalacticTrader.Desktop.Realtime;

public sealed class DashboardRealtimeSnapshotApiDto
{
    public DateTime CapturedAtUtc { get; init; }
    public DashboardRealtimeMetricsApiDto Metrics { get; init; } = new();
    public IReadOnlyList<DashboardRealtimeEventApiDto> Events { get; init; } = [];
}
