using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Battles;

public static class BattleFeedBuilder
{
    public static IReadOnlyList<BattleLogDisplayRow> Build(IReadOnlyList<CombatLogApiDto> logs)
    {
        return logs
            .OrderByDescending(static log => log.BattleEndedAt)
            .Select(log =>
            {
                var projection = BattleOutcomeProjector.Project(log);
                return new BattleLogDisplayRow
                {
                    EndedAtUtc = log.BattleEndedAt.ToUniversalTime(),
                    Outcome = log.BattleOutcome,
                    ReputationDelta = projection.ReputationDelta,
                    EconomicImpactProjection = projection.EconomicImpactProjection,
                    DamageReport = projection.DamageReport,
                    DurationSeconds = log.DurationSeconds,
                    TotalTicks = log.TotalTicks,
                    Attacker = log.AttackerId.ToString()[..8],
                    Defender = log.DefenderId?.ToString()[..8] ?? "-",
                    InsurancePayout = log.InsurancePayout
                };
            })
            .ToArray();
    }
}
