using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Battles;

public static class BattleOutcomeProjector
{
    public static BattleOutcomeProjection Project(CombatLogApiDto log)
    {
        var outcome = log.BattleOutcome ?? string.Empty;
        var canonicalOutcome = outcome.Trim();
        var normalized = canonicalOutcome.ToLowerInvariant();

        var baseImpact = log.InsurancePayout + (log.DurationSeconds * 12m) + (log.TotalTicks * 65m);
        var reputationDelta = ResolveReputationDelta(normalized);
        var impactMultiplier = ResolveImpactMultiplier(normalized);
        var economicImpact = Math.Round(baseImpact * impactMultiplier, 2, MidpointRounding.AwayFromZero);

        return new BattleOutcomeProjection
        {
            ReputationDelta = reputationDelta,
            EconomicImpactProjection = economicImpact,
            DamageReport = ResolveDamageReport(log)
        };
    }

    private static int ResolveReputationDelta(string normalizedOutcome)
    {
        if (normalizedOutcome.Contains("victory", StringComparison.Ordinal) ||
            normalizedOutcome.Contains("win", StringComparison.Ordinal))
        {
            return 2;
        }

        if (normalizedOutcome.Contains("defeat", StringComparison.Ordinal) ||
            normalizedOutcome.Contains("loss", StringComparison.Ordinal))
        {
            return -2;
        }

        if (normalizedOutcome.Contains("retreat", StringComparison.Ordinal))
        {
            return -1;
        }

        return 0;
    }

    private static decimal ResolveImpactMultiplier(string normalizedOutcome)
    {
        if (normalizedOutcome.Contains("victory", StringComparison.Ordinal) ||
            normalizedOutcome.Contains("win", StringComparison.Ordinal))
        {
            return 0.4m;
        }

        if (normalizedOutcome.Contains("retreat", StringComparison.Ordinal))
        {
            return -0.5m;
        }

        if (normalizedOutcome.Contains("defeat", StringComparison.Ordinal) ||
            normalizedOutcome.Contains("loss", StringComparison.Ordinal))
        {
            return -1.0m;
        }

        return -0.2m;
    }

    private static string ResolveDamageReport(CombatLogApiDto log)
    {
        var intensity = (log.TotalTicks * 6) + (log.DurationSeconds / 3) + (int)Math.Round(log.InsurancePayout / 750m);

        if (intensity >= 140)
        {
            return "Critical losses";
        }

        if (intensity >= 75)
        {
            return "Major damage";
        }

        if (intensity >= 35)
        {
            return "Moderate damage";
        }

        return "Minor damage";
    }
}
