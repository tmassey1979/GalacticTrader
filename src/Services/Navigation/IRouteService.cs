namespace GalacticTrader.Services.Navigation;

public interface IRouteService
{
    Task<IEnumerable<RouteDto>> GetAllRoutesAsync(CancellationToken cancellationToken = default);
    Task<RouteDto?> GetRouteByIdAsync(Guid routeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteDto>> GetOutboundRoutesAsync(Guid fromSectorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteDto>> GetInboundRoutesAsync(Guid toSectorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteDto>> GetRoutesBetweenAsync(
        Guid sectorAId,
        Guid sectorBId,
        CancellationToken cancellationToken = default);
    Task<RouteDto> CreateRouteAsync(
        Guid fromSectorId,
        Guid toSectorId,
        string legalStatus,
        string warpGateType,
        CancellationToken cancellationToken = default);
    Task<RouteDto?> UpdateRouteAsync(
        Guid routeId,
        string? legalStatus = null,
        float? baseRiskScore = null,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteRouteAsync(Guid routeId, CancellationToken cancellationToken = default);
    Task<double?> GetDistanceBetweenSectorsAsync(
        Guid sectorAId,
        Guid sectorBId,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteDto>> GetDangerousRoutesAsync(int riskThreshold = 70, CancellationToken cancellationToken = default);
    Task<IEnumerable<RouteDto>> GetLegalRoutesAsync(CancellationToken cancellationToken = default);
}
