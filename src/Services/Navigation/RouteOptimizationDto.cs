namespace GalacticTrader.Services.Navigation;

public sealed class RouteOptimizationDto
{
    public RoutePlanDto? Fastest { get; init; }
    public RoutePlanDto? Cheapest { get; init; }
    public RoutePlanDto? Safest { get; init; }
    public RoutePlanDto? Balanced { get; init; }
}
