using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Battles;

public sealed class BattlesDataSource
{
    public required Func<int, CancellationToken, Task<IReadOnlyList<CombatLogApiDto>>> LoadRecentLogsAsync { get; init; }

    public required Func<CancellationToken, Task<IReadOnlyList<CombatSummaryApiDto>>> LoadActiveCombatsAsync { get; init; }

    public required Func<StartCombatApiRequest, CancellationToken, Task<CombatSummaryApiDto>> StartCombatAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<CombatSummaryApiDto?>> LoadCombatAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<CombatTickResultApiDto?>> TickCombatAsync { get; init; }

    public required Func<Guid, CancellationToken, Task<CombatSummaryApiDto?>> EndCombatAsync { get; init; }
}
