using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Dashboard;

public sealed class DashboardDataSource
{
    public required Func<Guid, CancellationToken, Task<IReadOnlyList<TradeExecutionResultApiDto>>> LoadTransactionsAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<IReadOnlyList<ShipApiDto>>> LoadShipsAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<EscortSummaryApiDto?>> LoadEscortAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<IReadOnlyList<PlayerFactionStandingApiDto>>> LoadStandingsAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<IReadOnlyList<RouteApiDto>>> LoadDangerousRoutesAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<IReadOnlyList<IntelligenceReportApiDto>>> LoadIntelligenceAsync { get; init; }

    public required Func<CancellationToken, Task<GlobalMetricsSummaryApiDto?>> LoadGlobalMetricsAsync { get; init; }
}
