using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Routes;

public static class RoutePlanAssembler
{
    public static RoutePlanApiDto Combine(IReadOnlyList<RoutePlanApiDto> segments)
    {
        if (segments.Count == 0)
        {
            throw new InvalidOperationException("At least one route segment is required for assembly.");
        }

        var first = segments[0];
        var combinedPath = new List<Guid>();
        var combinedHops = new List<RouteHopApiDto>();
        double totalCost = 0;
        double totalFuel = 0;
        double totalRisk = 0;
        var totalTravelTimeSeconds = 0;

        foreach (var segment in segments)
        {
            totalCost += segment.TotalCost;
            totalFuel += segment.TotalFuelCost;
            totalRisk += segment.TotalRiskScore;
            totalTravelTimeSeconds += segment.TotalTravelTimeSeconds;
            combinedHops.AddRange(segment.Hops);
            AppendPath(combinedPath, segment.SectorPath);
        }

        return new RoutePlanApiDto
        {
            FromSectorId = first.FromSectorId,
            ToSectorId = segments[^1].ToSectorId,
            Algorithm = first.Algorithm,
            TravelMode = first.TravelMode,
            TotalCost = totalCost,
            TotalFuelCost = totalFuel,
            TotalRiskScore = totalRisk,
            TotalTravelTimeSeconds = totalTravelTimeSeconds,
            SectorPath = combinedPath,
            Hops = combinedHops
        };
    }

    private static void AppendPath(List<Guid> destination, IReadOnlyList<Guid> source)
    {
        if (source.Count == 0)
        {
            return;
        }

        if (destination.Count == 0)
        {
            destination.AddRange(source);
            return;
        }

        var startIndex = destination[^1] == source[0] ? 1 : 0;
        for (var i = startIndex; i < source.Count; i++)
        {
            destination.Add(source[i]);
        }
    }
}
