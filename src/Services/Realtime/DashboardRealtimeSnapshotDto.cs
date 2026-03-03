namespace GalacticTrader.Services.Realtime;

public sealed class DashboardRealtimeSnapshotDto
{
    public DateTime CapturedAtUtc { get; init; }
    public DashboardRealtimeMetricsDto Metrics { get; init; } = new();
    public IReadOnlyList<DashboardRealtimeEventDto> Events { get; init; } = [];
}
