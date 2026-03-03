using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class ReputationSanctionBonusProjector
{
    public static IReadOnlyList<ReputationSanctionBonusDisplayRow> Build(IReadOnlyList<PlayerFactionStandingApiDto> standings)
    {
        return standings
            .OrderByDescending(static standing => standing.ReputationScore)
            .Select(standing => new ReputationSanctionBonusDisplayRow
            {
                FactionId = standing.FactionId.ToString()[..8],
                Bonus = ResolveBonus(standing),
                Sanction = ResolveSanction(standing)
            })
            .ToArray();
    }

    private static string ResolveBonus(PlayerFactionStandingApiDto standing)
    {
        if (standing.ReputationScore >= 60)
        {
            return "Escort subsidy -10%";
        }

        if (standing.TradingDiscount > 0m)
        {
            return $"Trade discount {standing.TradingDiscount:P1}";
        }

        if (standing.HasAccess)
        {
            return "Standard market access";
        }

        return "None";
    }

    private static string ResolveSanction(PlayerFactionStandingApiDto standing)
    {
        if (!standing.HasAccess)
        {
            return "Market access restricted";
        }

        if (standing.ReputationScore <= -50)
        {
            return "Protection surcharge +15%";
        }

        if (standing.ReputationScore < 0)
        {
            return "Elevated inspection risk";
        }

        return "None";
    }
}
