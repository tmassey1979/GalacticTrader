using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Battles;

public sealed record BattlesModuleState(
    IReadOnlyList<CombatLogApiDto> RecentLogs,
    IReadOnlyList<CombatSummaryApiDto> ActiveCombats,
    BattleOutcomeSummary OutcomeSummary,
    DateTime LoadedAtUtc);
