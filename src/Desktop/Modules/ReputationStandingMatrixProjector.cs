using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class ReputationStandingMatrixProjector
{
    public static IReadOnlyList<ReputationStandingMatrixDisplayRow> Build(IReadOnlyList<PlayerFactionStandingApiDto> standings)
    {
        return standings
            .OrderByDescending(static standing => standing.ReputationScore)
            .Select(standing => new ReputationStandingMatrixDisplayRow
            {
                FactionId = standing.FactionId.ToString()[..8],
                ReputationScore = standing.ReputationScore,
                NormalizedScorePercent = Normalize(standing.ReputationScore),
                ScoreSummary = $"{standing.ReputationScore} [{standing.Tier}]"
            })
            .ToArray();
    }

    private static double Normalize(int reputationScore)
    {
        return Math.Round(Math.Clamp((reputationScore + 100d) / 2d, 0d, 100d), 1, MidpointRounding.AwayFromZero);
    }
}
