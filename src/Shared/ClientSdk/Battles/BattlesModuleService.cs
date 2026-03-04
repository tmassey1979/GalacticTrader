using System.Text.RegularExpressions;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Battles;

public sealed class BattlesModuleService
{
    private static readonly Regex DurationRegex = new(@"Duration\s+(?<seconds>\d+)s", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InsuranceRegex = new(@"Insurance\s+(?<amount>[-+]?\d+(?:\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly BattlesDataSource _dataSource;

    public BattlesModuleService(BattlesDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<BattlesModuleState> LoadStateAsync(
        int logLimit = 60,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(logLimit, 1, 400);
        var logsTask = _dataSource.LoadRecentLogsAsync(normalizedLimit, cancellationToken);
        var activeCombatsTask = _dataSource.LoadActiveCombatsAsync(cancellationToken);

        await Task.WhenAll(logsTask, activeCombatsTask);
        var logs = await logsTask;
        var activeCombats = await activeCombatsTask;
        var summary = BuildOutcomeSummary(logs);
        return new BattlesModuleState(logs, activeCombats, summary, DateTime.UtcNow);
    }

    public Task<CombatSummaryApiDto> StartCombatAsync(
        StartCombatApiRequest request,
        CancellationToken cancellationToken = default)
    {
        return _dataSource.StartCombatAsync(request, cancellationToken);
    }

    public Task<CombatSummaryApiDto?> LoadCombatAsync(
        Guid combatId,
        CancellationToken cancellationToken = default)
    {
        return _dataSource.LoadCombatAsync(combatId, cancellationToken);
    }

    public Task<CombatTickResultApiDto?> TickCombatAsync(
        Guid combatId,
        CancellationToken cancellationToken = default)
    {
        return _dataSource.TickCombatAsync(combatId, cancellationToken);
    }

    public Task<CombatSummaryApiDto?> EndCombatAsync(
        Guid combatId,
        CancellationToken cancellationToken = default)
    {
        return _dataSource.EndCombatAsync(combatId, cancellationToken);
    }

    public BattlesModuleState ApplyRealtimeSnapshot(
        BattlesModuleState currentState,
        DashboardRealtimeSnapshotApiDto snapshot,
        int maxLogs = 120)
    {
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(snapshot);

        var combatEvents = snapshot.Events
            .Where(static evt => evt.Category.Equals("Combat", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (combatEvents.Length == 0)
        {
            return currentState;
        }

        var existing = currentState.RecentLogs.ToList();
        var seenKeys = existing
            .Select(CreateRealtimeKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var combatEvent in combatEvents)
        {
            var mapped = MapRealtimeEventToCombatLog(combatEvent);
            if (seenKeys.Add(CreateRealtimeKey(mapped)))
            {
                existing.Add(mapped);
            }
        }

        var mergedLogs = existing
            .OrderByDescending(static log => log.BattleEndedAt)
            .ThenByDescending(static log => log.Id)
            .Take(Math.Max(1, maxLogs))
            .ToArray();

        return new BattlesModuleState(
            mergedLogs,
            currentState.ActiveCombats,
            BuildOutcomeSummary(mergedLogs),
            snapshot.CapturedAtUtc);
    }

    public static BattleOutcomeSummary BuildOutcomeSummary(IReadOnlyList<CombatLogApiDto> logs)
    {
        var total = logs.Count;
        var victories = logs.Count(static log => log.BattleOutcome.Contains("victory", StringComparison.OrdinalIgnoreCase));
        var defeats = logs.Count(static log => log.BattleOutcome.Contains("defeat", StringComparison.OrdinalIgnoreCase) ||
                                                log.BattleOutcome.Contains("loss", StringComparison.OrdinalIgnoreCase));
        var others = total - victories - defeats;
        var averageDuration = total == 0
            ? 0d
            : logs.Average(static log => Math.Max(log.DurationSeconds, 0));
        var insurancePayout = logs.Sum(static log => log.InsurancePayout);
        DateTime? latestEnded = logs.Count == 0
            ? null
            : logs.Max(static log => log.BattleEndedAt.ToUniversalTime());

        return new BattleOutcomeSummary(
            TotalBattles: total,
            VictoryCount: victories,
            DefeatCount: defeats,
            OtherOutcomeCount: others,
            AverageDurationSeconds: Math.Round(averageDuration, 1),
            TotalInsurancePayout: insurancePayout,
            LastBattleEndedUtc: latestEnded);
    }

    private static string CreateRealtimeKey(CombatLogApiDto log)
    {
        return $"{log.BattleEndedAt.ToUniversalTime():O}|{log.BattleOutcome}";
    }

    private static CombatLogApiDto MapRealtimeEventToCombatLog(DashboardRealtimeEventApiDto evt)
    {
        var durationMatch = DurationRegex.Match(evt.Detail);
        var durationSeconds = durationMatch.Success &&
                              int.TryParse(durationMatch.Groups["seconds"].Value, out var parsedDuration)
            ? Math.Max(parsedDuration, 0)
            : 0;

        var insuranceMatch = InsuranceRegex.Match(evt.Detail);
        var insurancePayout = insuranceMatch.Success &&
                              decimal.TryParse(insuranceMatch.Groups["amount"].Value, out var parsedPayout)
            ? parsedPayout
            : 0m;

        return new CombatLogApiDto
        {
            Id = Guid.NewGuid(),
            AttackerId = Guid.Empty,
            DefenderId = null,
            AttackerShipId = Guid.Empty,
            DefenderShipId = null,
            BattleOutcome = string.IsNullOrWhiteSpace(evt.Title) ? "Combat Event" : evt.Title.Trim(),
            BattleStartedAt = evt.OccurredAtUtc.ToUniversalTime().AddSeconds(-durationSeconds),
            BattleEndedAt = evt.OccurredAtUtc.ToUniversalTime(),
            DurationSeconds = durationSeconds,
            TotalTicks = 0,
            InsurancePayout = insurancePayout
        };
    }
}
