namespace GalacticTrader.Services.Navigation;

public interface IRoutePlanningService
{
    Task<RoutePlanDto?> CalculateRouteAsync(
        Guid fromSectorId,
        Guid toSectorId,
        TravelMode travelMode = TravelMode.Standard,
        string algorithm = "dijkstra",
        CancellationToken cancellationToken = default);

    Task<RouteOptimizationDto> GetOptimizedRoutesAsync(
        Guid fromSectorId,
        Guid toSectorId,
        CancellationToken cancellationToken = default);
}
