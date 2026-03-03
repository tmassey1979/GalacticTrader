using GalacticTrader.Services.Combat;
using GalacticTrader.Services.Fleet;
using GalacticTrader.Services.Market;
using GalacticTrader.Services.Navigation;
using GalacticTrader.Services.Reputation;
using GalacticTrader.Services.Strategic;

namespace GalacticTrader.Services.Realtime;

public sealed class DashboardRealtimeSnapshotService : IDashboardRealtimeSnapshotService
{
    private readonly IMarketTransactionService _marketService;
    private readonly IReputationService _reputationService;
    private readonly IFleetService _fleetService;
    private readonly IRouteService _routeService;
    private readonly IStrategicSystemsService _strategicService;
    private readonly ICombatService _combatService;

    public DashboardRealtimeSnapshotService(
        IMarketTransactionService marketService,
        IReputationService reputationService,
        IFleetService fleetService,
        IRouteService routeService,
        IStrategicSystemsService strategicService,
        ICombatService combatService)
    {
        _marketService = marketService;
        _reputationService = reputationService;
        _fleetService = fleetService;
        _routeService = routeService;
        _strategicService = strategicService;
        _combatService = combatService;
    }

    public async Task<DashboardRealtimeSnapshotDto> BuildSnapshotAsync(
        Guid playerId,
        int riskThreshold = 65,
        int transactionLimit = 25,
        int combatLimit = 25,
        CancellationToken cancellationToken = default)
    {
        var transactionsTask = _marketService.GetPlayerTransactionsAsync(playerId, transactionLimit, cancellationToken);
        var standingsTask = _reputationService.GetFactionStandingsAsync(playerId, cancellationToken);
        var escortTask = _fleetService.GetEscortSummaryAsync(playerId, FleetFormation.Defensive, cancellationToken);
        var routesTask = _routeService.GetAllRoutesAsync(cancellationToken);
        var dangerousRoutesTask = _routeService.GetDangerousRoutesAsync(riskThreshold, cancellationToken);
        var reportsTask = _strategicService.GetIntelligenceReportsAsync(playerId, null, cancellationToken);
        var combatLogsTask = _combatService.GetRecentCombatLogsAsync(combatLimit, cancellationToken);

        await Task.WhenAll(transactionsTask, standingsTask, escortTask, routesTask, dangerousRoutesTask, reportsTask, combatLogsTask);

        return DashboardRealtimeSnapshotProjection.Build(
            transactionsTask.Result,
            standingsTask.Result,
            escortTask.Result,
            routesTask.Result.ToArray(),
            dangerousRoutesTask.Result.ToArray(),
            reportsTask.Result,
            combatLogsTask.Result,
            DateTime.UtcNow);
    }
}
