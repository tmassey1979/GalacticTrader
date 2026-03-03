using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Battles;

public static class BattleOutcomeProjector
{
    public static BattleOutcomeProjection Project(CombatLogApiDto log)
    {
        var outcome = log.BattleOutcome ?? string.Empty;
        var canonicalOutcome = outcome.Trim();
        var normalized = canonicalOutcome.ToLowerInvariant();
        var ratings = ResolveRatings(log, normalized);

        var baseImpact = log.InsurancePayout + (log.DurationSeconds * 12m) + (log.TotalTicks * 65m);
        var reputationDelta = ResolveReputationDelta(normalized);
        var impactMultiplier = ResolveImpactMultiplier(normalized);
        var economicImpact = Math.Round(baseImpact * impactMultiplier, 2, MidpointRounding.AwayFromZero);
        var resourceChange = ResolveResourceChange(log, impactMultiplier);
        var environmentalModifier = ResolveEnvironmentalModifier(log);
        var protectionModifier = ResolveProtectionModifier(log);

        return new BattleOutcomeProjection
        {
            AttackerRating = ratings.AttackerRating,
            DefenderRating = ratings.DefenderRating,
            ReputationDelta = reputationDelta,
            ResourceChange = resourceChange,
            EnvironmentalModifier = environmentalModifier,
            ProtectionModifier = protectionModifier,
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

    private static decimal ResolveEnvironmentalModifier(CombatLogApiDto log)
    {
        var volatility = ((log.TotalTicks * 1.2m) + (log.DurationSeconds / 10m)) / 100m;
        var shifted = volatility - 0.10m;
        return Math.Round(Math.Clamp(shifted, -0.15m, 0.35m), 3, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveProtectionModifier(CombatLogApiDto log)
    {
        var durabilitySignal = ((log.TotalTicks * 8m) + (log.DurationSeconds / 4m)) / 300m;
        var insurancePenalty = log.InsurancePayout / 20_000m;
        var modifier = (durabilitySignal * 0.25m) - insurancePenalty;
        return Math.Round(Math.Clamp(modifier, -0.25m, 0.25m), 3, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveResourceChange(CombatLogApiDto log, decimal impactMultiplier)
    {
        var baseline = (log.DurationSeconds * 8m) + (log.TotalTicks * 40m);
        var insuranceOffset = log.InsurancePayout * 0.12m;
        var signed = (baseline + insuranceOffset) * impactMultiplier;
        return Math.Round(signed, 2, MidpointRounding.AwayFromZero);
    }

    private static (int AttackerRating, int DefenderRating) ResolveRatings(CombatLogApiDto log, string normalizedOutcome)
    {
        var baseline = 100 + (log.TotalTicks * 4) + (log.DurationSeconds / 3);
        var attacker = baseline;
        var defender = baseline;

        if (normalizedOutcome.Contains("victory", StringComparison.Ordinal) ||
            normalizedOutcome.Contains("win", StringComparison.Ordinal))
        {
            attacker += 15;
            defender -= 10;
        }
        else if (normalizedOutcome.Contains("defeat", StringComparison.Ordinal) ||
                 normalizedOutcome.Contains("loss", StringComparison.Ordinal))
        {
            attacker -= 10;
            defender += 15;
        }
        else if (normalizedOutcome.Contains("retreat", StringComparison.Ordinal))
        {
            attacker -= 5;
            defender += 5;
        }

        var insurancePenalty = (int)Math.Round(log.InsurancePayout / 150m, MidpointRounding.AwayFromZero);
        attacker -= insurancePenalty;
        defender -= insurancePenalty / 2;

        attacker = Math.Clamp(attacker, 0, 500);
        defender = Math.Clamp(defender, 0, 500);
        return (attacker, defender);
    }
}
