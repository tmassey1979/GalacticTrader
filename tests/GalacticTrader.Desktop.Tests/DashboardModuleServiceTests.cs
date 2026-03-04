using GalacticTrader.ClientSdk.Dashboard;
using GalacticTrader.ClientSdk.Shell;
using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardModuleServiceTests
{
    [Fact]
    public async Task LoadBoardAsync_AggregatesSnapshotAndPrioritizedActions()
    {
        var playerId = Guid.NewGuid();
        var dataSource = new DashboardDataSource
        {
            LoadTransactionsAsync = (_, _) => Task.FromResult<IReadOnlyList<TradeExecutionResultApiDto>>(
            [
                new TradeExecutionResultApiDto
                {
                    RemainingPlayerCredits = 12000m
                }
            ]),
            LoadShipsAsync = (_, _) => Task.FromResult<IReadOnlyList<ShipApiDto>>(
            [
                new ShipApiDto { Id = Guid.NewGuid() }
            ]),
            LoadEscortAsync = (_, _) => Task.FromResult<EscortSummaryApiDto?>(new EscortSummaryApiDto
            {
                FleetStrength = 10,
                EscortStrength = 2
            }),
            LoadStandingsAsync = (_, _) => Task.FromResult<IReadOnlyList<PlayerFactionStandingApiDto>>(
            [
                new PlayerFactionStandingApiDto
                {
                    ReputationScore = -15
                }
            ]),
            LoadDangerousRoutesAsync = (_, _) => Task.FromResult<IReadOnlyList<RouteApiDto>>(
            [
                new RouteApiDto { Id = Guid.NewGuid() },
                new RouteApiDto { Id = Guid.NewGuid() }
            ]),
            LoadIntelligenceAsync = (_, _) => Task.FromResult<IReadOnlyList<IntelligenceReportApiDto>>(
            [
                new IntelligenceReportApiDto { IsExpired = false },
                new IntelligenceReportApiDto { IsExpired = true }
            ]),
            LoadGlobalMetricsAsync = _ => Task.FromResult<GlobalMetricsSummaryApiDto?>(new GlobalMetricsSummaryApiDto
            {
                EconomicStabilityIndex = 40m,
                ActivePlayers24h = 55
            })
        };
        var service = new DashboardModuleService(dataSource);

        var board = await service.LoadBoardAsync(playerId);

        Assert.Equal(12000m, board.Snapshot.AvailableCredits);
        Assert.Equal(1, board.Snapshot.ShipCount);
        Assert.Equal(10, board.Snapshot.FleetStrength);
        Assert.Equal(2, board.Snapshot.EscortStrength);
        Assert.Equal(2, board.Snapshot.DangerousRouteCount);
        Assert.Equal(1, board.Snapshot.ActiveIntelligenceCount);
        Assert.Equal(-15, board.Snapshot.ReputationScore);
        Assert.Equal(40m, board.Snapshot.EconomicStabilityIndex);
        Assert.Equal(55, board.Snapshot.ActivePlayers24h);

        Assert.Equal(DashboardActionType.ImproveLiquidity, board.Actions[0].ActionType);
        Assert.Contains(board.Actions, action => action.ActionType == DashboardActionType.ReviewDangerousRoutes);
        Assert.Contains(board.Actions, action => action.ActionType == DashboardActionType.ReviewIntelligence);
        Assert.Contains(board.Actions, action => action.ActionType == DashboardActionType.RepairReputation);
        Assert.Contains(board.Actions, action => action.TargetModule == GameplayModuleId.Trading);
    }

    [Fact]
    public void Build_WithNoPressureSignals_ReturnsExpansionAction()
    {
        var snapshot = new DashboardSnapshot(
            AvailableCredits: 100000m,
            ShipCount: 4,
            FleetStrength: 8,
            EscortStrength: 8,
            DangerousRouteCount: 0,
            ActiveIntelligenceCount: 0,
            ReputationScore: 120,
            EconomicStabilityIndex: 85m,
            ActivePlayers24h: 80);

        var actions = DashboardActionPlanner.Build(snapshot);

        Assert.Single(actions);
        Assert.Equal(DashboardActionType.ExpandTradeNetwork, actions[0].ActionType);
        Assert.Equal(GameplayModuleId.Routes, actions[0].TargetModule);
    }

    [Fact]
    public void Build_WhenNoShips_PrioritizesAcquireShip()
    {
        var snapshot = new DashboardSnapshot(
            AvailableCredits: 500000m,
            ShipCount: 0,
            FleetStrength: 0,
            EscortStrength: 0,
            DangerousRouteCount: 0,
            ActiveIntelligenceCount: 0,
            ReputationScore: 0,
            EconomicStabilityIndex: 60m,
            ActivePlayers24h: 20);

        var actions = DashboardActionPlanner.Build(snapshot);

        Assert.Equal(DashboardActionType.AcquireShip, actions[0].ActionType);
        Assert.Equal(GameplayModuleId.Fleet, actions[0].TargetModule);
    }
}
