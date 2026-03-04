namespace GalacticTrader.ClientSdk.Dashboard;

public sealed class DashboardModuleService
{
    private readonly DashboardDataSource _dataSource;

    public DashboardModuleService(DashboardDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<DashboardActionBoard> LoadBoardAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var transactionsTask = _dataSource.LoadTransactionsAsync(playerId, cancellationToken);
        var shipsTask = _dataSource.LoadShipsAsync(playerId, cancellationToken);
        var escortTask = _dataSource.LoadEscortAsync(playerId, cancellationToken);
        var standingsTask = _dataSource.LoadStandingsAsync(playerId, cancellationToken);
        var dangerousRoutesTask = _dataSource.LoadDangerousRoutesAsync(playerId, cancellationToken);
        var intelligenceTask = _dataSource.LoadIntelligenceAsync(playerId, cancellationToken);
        var globalMetricsTask = _dataSource.LoadGlobalMetricsAsync(cancellationToken);

        await Task.WhenAll(
            transactionsTask,
            shipsTask,
            escortTask,
            standingsTask,
            dangerousRoutesTask,
            intelligenceTask,
            globalMetricsTask);

        var snapshot = BuildSnapshot(
            await transactionsTask,
            await shipsTask,
            await escortTask,
            await standingsTask,
            await dangerousRoutesTask,
            await intelligenceTask,
            await globalMetricsTask);
        var actions = DashboardActionPlanner.Build(snapshot);
        return new DashboardActionBoard(snapshot, actions);
    }

    internal static DashboardSnapshot BuildSnapshot(
        IReadOnlyList<GalacticTrader.Desktop.Api.TradeExecutionResultApiDto> transactions,
        IReadOnlyList<GalacticTrader.Desktop.Api.ShipApiDto> ships,
        GalacticTrader.Desktop.Api.EscortSummaryApiDto? escort,
        IReadOnlyList<GalacticTrader.Desktop.Api.PlayerFactionStandingApiDto> standings,
        IReadOnlyList<GalacticTrader.Desktop.Api.RouteApiDto> dangerousRoutes,
        IReadOnlyList<GalacticTrader.Desktop.Api.IntelligenceReportApiDto> intelligenceReports,
        GalacticTrader.Desktop.Api.GlobalMetricsSummaryApiDto? globalMetrics)
    {
        var credits = transactions
            .Select(static transaction => transaction.RemainingPlayerCredits)
            .FirstOrDefault();

        var reputationScore = standings.Count == 0
            ? 0
            : standings.Max(static standing => standing.ReputationScore);

        var activeIntelCount = intelligenceReports.Count(static report => !report.IsExpired);

        return new DashboardSnapshot(
            AvailableCredits: credits,
            ShipCount: ships.Count,
            FleetStrength: escort?.FleetStrength ?? 0,
            EscortStrength: escort?.EscortStrength ?? 0,
            DangerousRouteCount: dangerousRoutes.Count,
            ActiveIntelligenceCount: activeIntelCount,
            ReputationScore: reputationScore,
            EconomicStabilityIndex: globalMetrics?.EconomicStabilityIndex ?? 0m,
            ActivePlayers24h: globalMetrics?.ActivePlayers24h ?? 0);
    }
}
