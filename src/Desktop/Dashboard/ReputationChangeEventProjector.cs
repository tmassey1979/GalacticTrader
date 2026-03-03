using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class ReputationChangeEventProjector
{
    public static IReadOnlyList<EventFeedEntry> Build(
        IReadOnlyList<PlayerFactionStandingApiDto> standings,
        DateTime capturedAtUtc)
    {
        return standings
            .Where(static standing =>
                Math.Abs(standing.ReputationScore) >= 35 ||
                standing.TradingDiscount >= 0.08m ||
                !standing.HasAccess)
            .OrderByDescending(static standing => Math.Abs(standing.ReputationScore))
            .Take(5)
            .Select(standing => new EventFeedEntry
            {
                OccurredAtUtc = capturedAtUtc,
                Category = "Reputation",
                Title = BuildTitle(standing),
                Detail =
                    $"Score {standing.ReputationScore:+0;-0;0} | Tier {NormalizeTier(standing.Tier)} | " +
                    $"Access {standing.HasAccess} | Discount {standing.TradingDiscount:P1}"
            })
            .ToArray();
    }

    private static string BuildTitle(PlayerFactionStandingApiDto standing)
    {
        var factionToken = standing.FactionId == Guid.Empty
            ? "Unknown"
            : standing.FactionId.ToString()[..8];

        if (!standing.HasAccess || standing.ReputationScore <= -45)
        {
            return $"{factionToken} sanctions pressure";
        }

        if (standing.ReputationScore >= 60 || standing.TradingDiscount >= 0.10m)
        {
            return $"{factionToken} alliance access expanded";
        }

        return $"{factionToken} standing shift";
    }

    private static string NormalizeTier(string tier)
    {
        return string.IsNullOrWhiteSpace(tier)
            ? "Unrated"
            : tier;
    }
}
