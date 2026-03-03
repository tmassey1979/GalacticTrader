using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Realtime;

public sealed class DashboardRealtimeProjectionResult
{
    public StatusMetricSnapshot Metrics { get; init; } = new();
    public IReadOnlyList<EventFeedEntry> Events { get; init; } = [];
}
