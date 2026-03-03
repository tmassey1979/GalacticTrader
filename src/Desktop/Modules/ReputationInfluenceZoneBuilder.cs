using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class ReputationInfluenceZoneBuilder
{
    public static IReadOnlyList<string> Build(IReadOnlyList<PlayerFactionStandingApiDto> standings)
    {
        if (standings.Count == 0)
        {
            return ["No influence zones available."];
        }

        var stronghold = standings.Count(static standing => standing.ReputationScore >= 70);
        var contested = standings.Count(static standing => standing.ReputationScore is >= 30 and < 70);
        var neutral = standings.Count(static standing => standing.ReputationScore is >= 0 and < 30);
        var hostile = standings.Count(static standing => standing.ReputationScore < 0);

        return
        [
            $"Stronghold Zones: {stronghold}",
            $"Contested Zones: {contested}",
            $"Neutral Zones: {neutral}",
            $"Hostile Zones: {hostile}"
        ];
    }
}
