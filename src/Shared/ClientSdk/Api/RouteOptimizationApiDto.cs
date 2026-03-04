namespace GalacticTrader.Desktop.Api;

public sealed class RouteOptimizationApiDto
{
    public RoutePlanApiDto? Fastest { get; init; }
    public RoutePlanApiDto? Cheapest { get; init; }
    public RoutePlanApiDto? Safest { get; init; }
    public RoutePlanApiDto? Balanced { get; init; }
}
