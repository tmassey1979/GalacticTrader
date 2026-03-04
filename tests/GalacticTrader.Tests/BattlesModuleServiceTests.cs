using GalacticTrader.ClientSdk.Battles;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.Tests;

public sealed class BattlesModuleServiceTests
{
    [Fact]
    public async Task LoadStateAsync_BuildsBattleOutcomeSummary()
    {
        var logs = new[]
        {
            new CombatLogApiDto
            {
                Id = Guid.NewGuid(),
                BattleOutcome = "Victory",
                BattleStartedAt = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc),
                BattleEndedAt = new DateTime(2026, 3, 4, 10, 0, 45, DateTimeKind.Utc),
                DurationSeconds = 45,
                InsurancePayout = 12m
            },
            new CombatLogApiDto
            {
                Id = Guid.NewGuid(),
                BattleOutcome = "Defeat",
                BattleStartedAt = new DateTime(2026, 3, 4, 11, 0, 0, DateTimeKind.Utc),
                BattleEndedAt = new DateTime(2026, 3, 4, 11, 1, 10, DateTimeKind.Utc),
                DurationSeconds = 70,
                InsurancePayout = 0m
            }
        };
        var active = new[]
        {
            new CombatSummaryApiDto { CombatId = Guid.NewGuid(), State = 1, TickCount = 7, MaxTicks = 600 }
        };
        var service = CreateService(logs: logs, active: active);

        var state = await service.LoadStateAsync();

        Assert.Equal(2, state.OutcomeSummary.TotalBattles);
        Assert.Equal(1, state.OutcomeSummary.VictoryCount);
        Assert.Equal(1, state.OutcomeSummary.DefeatCount);
        Assert.Equal(57.5d, state.OutcomeSummary.AverageDurationSeconds);
        Assert.Equal(12m, state.OutcomeSummary.TotalInsurancePayout);
        Assert.Single(state.ActiveCombats);
    }

    [Fact]
    public void ApplyRealtimeSnapshot_AppendsCombatEventsAndRecalculatesSummary()
    {
        var initialLog = new CombatLogApiDto
        {
            Id = Guid.NewGuid(),
            BattleOutcome = "Victory",
            BattleStartedAt = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc),
            BattleEndedAt = new DateTime(2026, 3, 4, 10, 0, 30, DateTimeKind.Utc),
            DurationSeconds = 30,
            InsurancePayout = 4m
        };
        var service = CreateService();
        var initialState = new BattlesModuleState(
            RecentLogs: [initialLog],
            ActiveCombats: [],
            OutcomeSummary: BattlesModuleService.BuildOutcomeSummary([initialLog]),
            LoadedAtUtc: initialLog.BattleEndedAt);
        var snapshot = new DashboardRealtimeSnapshotApiDto
        {
            CapturedAtUtc = new DateTime(2026, 3, 4, 12, 0, 0, DateTimeKind.Utc),
            Events =
            [
                new DashboardRealtimeEventApiDto
                {
                    OccurredAtUtc = new DateTime(2026, 3, 4, 11, 59, 50, DateTimeKind.Utc),
                    Category = "Combat",
                    Title = "Defeat",
                    Detail = "Duration 50s | Ticks 10 | Insurance 9"
                }
            ]
        };

        var projected = service.ApplyRealtimeSnapshot(initialState, snapshot, maxLogs: 10);

        Assert.Equal(2, projected.RecentLogs.Count);
        Assert.Equal(2, projected.OutcomeSummary.TotalBattles);
        Assert.Equal(1, projected.OutcomeSummary.VictoryCount);
        Assert.Equal(1, projected.OutcomeSummary.DefeatCount);
        Assert.Equal(13m, projected.OutcomeSummary.TotalInsurancePayout);
    }

    [Fact]
    public async Task StartCombatAsync_ForwardsRequestToDataSource()
    {
        var expected = new CombatSummaryApiDto
        {
            CombatId = Guid.NewGuid(),
            State = 1,
            TickCount = 0,
            MaxTicks = 600
        };
        StartCombatApiRequest? captured = null;
        var service = CreateService(startCombat: request =>
        {
            captured = request;
            return expected;
        });
        var requestPayload = new StartCombatApiRequest
        {
            AttackerShipId = Guid.NewGuid(),
            DefenderShipId = Guid.NewGuid(),
            MaxTicks = 900
        };

        var combat = await service.StartCombatAsync(requestPayload);

        Assert.NotNull(captured);
        Assert.Equal(requestPayload.AttackerShipId, captured!.AttackerShipId);
        Assert.Equal(requestPayload.DefenderShipId, captured.DefenderShipId);
        Assert.Equal(900, captured.MaxTicks);
        Assert.Equal(expected.CombatId, combat.CombatId);
    }

    private static BattlesModuleService CreateService(
        IReadOnlyList<CombatLogApiDto>? logs = null,
        IReadOnlyList<CombatSummaryApiDto>? active = null,
        Func<StartCombatApiRequest, CombatSummaryApiDto>? startCombat = null)
    {
        logs ??= [];
        active ??= [];
        startCombat ??= _ => new CombatSummaryApiDto { CombatId = Guid.NewGuid(), State = 1 };

        return new BattlesModuleService(new BattlesDataSource
        {
            LoadRecentLogsAsync = (_, _) => Task.FromResult(logs),
            LoadActiveCombatsAsync = _ => Task.FromResult(active),
            StartCombatAsync = (request, _) => Task.FromResult(startCombat.Invoke(request)),
            LoadCombatAsync = (_, _) => Task.FromResult<CombatSummaryApiDto?>(null),
            TickCombatAsync = (_, _) => Task.FromResult<CombatTickResultApiDto?>(null),
            EndCombatAsync = (_, _) => Task.FromResult<CombatSummaryApiDto?>(null)
        });
    }
}
