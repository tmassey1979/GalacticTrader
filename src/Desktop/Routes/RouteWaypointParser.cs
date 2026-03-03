namespace GalacticTrader.Desktop.Routes;

public static class RouteWaypointParser
{
    public static bool TryParse(
        string? raw,
        IReadOnlyList<RouteSectorSelectionItem> knownSectors,
        Guid fromSectorId,
        Guid toSectorId,
        out IReadOnlyList<Guid> waypointSectorIds,
        out string? error)
    {
        waypointSectorIds = [];
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var index = knownSectors
            .GroupBy(static sector => Normalize(sector.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        var parsed = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var token in SplitTokens(raw))
        {
            var key = Normalize(token);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!index.TryGetValue(key, out var matchedSector))
            {
                error = $"Waypoint '{token.Trim()}' does not match a known sector name.";
                return false;
            }

            if (matchedSector.SectorId == fromSectorId || matchedSector.SectorId == toSectorId)
            {
                error = $"Waypoint '{matchedSector.Name}' cannot be the same as origin or destination.";
                return false;
            }

            if (!seen.Add(matchedSector.SectorId))
            {
                error = $"Waypoint '{matchedSector.Name}' is duplicated.";
                return false;
            }

            parsed.Add(matchedSector.SectorId);
        }

        waypointSectorIds = parsed;
        return true;
    }

    private static IEnumerable<string> SplitTokens(string raw)
    {
        return raw.Split([',', ';', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string Normalize(string value) => value.Trim();
}
