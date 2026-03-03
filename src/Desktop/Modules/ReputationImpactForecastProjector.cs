using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class ReputationImpactForecastProjector
{
    public static ReputationImpactForecastProjection Build(
        IReadOnlyList<PlayerFactionStandingApiDto> standings,
        AlignmentAccessApiDto? access)
    {
        var trustedCount = standings.Count(static standing => standing.ReputationScore >= 35);
        var hostileCount = standings.Count(static standing => standing.ReputationScore <= -25);
        var allianceReadyCount = standings.Count(static standing => standing.HasAccess && standing.ReputationScore >= 20);
        var averageDiscount = standings.Count == 0
            ? 0m
            : standings.Average(static standing => standing.TradingDiscount);

        var tradeMarginModifier =
            averageDiscount +
            (trustedCount * 0.0125m) -
            (hostileCount * 0.01m) +
            ResolveTradePathModifier(access);
        tradeMarginModifier = decimal.Round(Math.Clamp(tradeMarginModifier, -0.20m, 0.35m), 4, MidpointRounding.AwayFromZero);

        var legalInsuranceModifier = access is null
            ? 0m
            : access.CanUseLegalInsurance
                ? -0.05m
                : 0.04m;
        var protectionCostModifier =
            (decimal)(access?.InsuranceCostModifier ?? 0f) +
            (hostileCount * 0.045m) -
            (trustedCount * 0.02m) +
            legalInsuranceModifier;
        protectionCostModifier = decimal.Round(Math.Clamp(protectionCostModifier, -0.40m, 0.80m), 4, MidpointRounding.AwayFromZero);

        var smugglingSuccessChance =
            0.35m +
            (access?.CanAccessBlackMarket == true ? 0.25m : -0.08m) +
            (IsOutlawPath(access) ? 0.12m : -0.02m) +
            (hostileCount * 0.02m) -
            (trustedCount * 0.015m);
        smugglingSuccessChance = decimal.Round(Math.Clamp(smugglingSuccessChance, 0.05m, 0.95m), 4, MidpointRounding.AwayFromZero);

        return new ReputationImpactForecastProjection
        {
            TradeMarginModifier = tradeMarginModifier,
            ProtectionCostModifier = protectionCostModifier,
            SmugglingSuccessChance = smugglingSuccessChance,
            AllianceAccessSummary = BuildAllianceAccessSummary(allianceReadyCount, standings.Count, access)
        };
    }

    private static decimal ResolveTradePathModifier(AlignmentAccessApiDto? access)
    {
        if (access is null)
        {
            return 0m;
        }

        if (string.Equals(access.Path, "Outlaw", StringComparison.OrdinalIgnoreCase))
        {
            return 0.03m;
        }

        if (string.Equals(access.Path, "Lawful", StringComparison.OrdinalIgnoreCase))
        {
            return 0.015m;
        }

        if (string.Equals(access.Path, "Neutral", StringComparison.OrdinalIgnoreCase))
        {
            return 0.005m;
        }

        return 0m;
    }

    private static bool IsOutlawPath(AlignmentAccessApiDto? access)
    {
        return access is not null &&
               string.Equals(access.Path, "Outlaw", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAllianceAccessSummary(
        int allianceReadyCount,
        int totalFactions,
        AlignmentAccessApiDto? access)
    {
        if (totalFactions <= 0)
        {
            return "No standings";
        }

        var ratio = (decimal)allianceReadyCount / totalFactions;
        var band = ratio switch
        {
            >= 0.70m => "Broad",
            >= 0.40m => "Selective",
            _ => "Limited"
        };

        var pathSuffix = string.IsNullOrWhiteSpace(access?.Path)
            ? string.Empty
            : $" - {access.Path} path";
        return $"{band} ({allianceReadyCount}/{totalFactions} factions){pathSuffix}";
    }
}
