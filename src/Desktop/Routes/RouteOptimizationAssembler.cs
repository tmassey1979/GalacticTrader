using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Routes;

public static class RouteOptimizationAssembler
{
    public static RouteOptimizationApiDto Combine(IReadOnlyList<RouteOptimizationApiDto> segmentOptimizations)
    {
        return new RouteOptimizationApiDto
        {
            Fastest = CombineProfile(segmentOptimizations.Select(static optimization => optimization.Fastest).ToArray()),
            Cheapest = CombineProfile(segmentOptimizations.Select(static optimization => optimization.Cheapest).ToArray()),
            Safest = CombineProfile(segmentOptimizations.Select(static optimization => optimization.Safest).ToArray()),
            Balanced = CombineProfile(segmentOptimizations.Select(static optimization => optimization.Balanced).ToArray())
        };
    }

    private static RoutePlanApiDto? CombineProfile(IReadOnlyList<RoutePlanApiDto?> profiles)
    {
        if (profiles.Count == 0)
        {
            return null;
        }

        if (profiles.Any(static profile => profile is null))
        {
            return null;
        }

        var segments = profiles
            .Select(static profile => profile!)
            .ToArray();
        return RoutePlanAssembler.Combine(segments);
    }
}
