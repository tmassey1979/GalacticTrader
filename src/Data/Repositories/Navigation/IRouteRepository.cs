namespace GalacticTrader.Data.Repositories.Navigation;

using GalacticTrader.Data.Models;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Route?> GetByIdAsync(Guid routeId, CancellationToken cancellationToken = default);
    Task<List<Route>> GetBySectorIdAsync(Guid sectorId, CancellationToken cancellationToken = default);
    Task<List<Route>> GetOutboundAsync(Guid fromSectorId, CancellationToken cancellationToken = default);
    Task<List<Route>> GetInboundAsync(Guid toSectorId, CancellationToken cancellationToken = default);
    Task<List<Route>> GetBetweenAsync(Guid sectorAId, Guid sectorBId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid fromSectorId, Guid toSectorId, CancellationToken cancellationToken = default);
    Task AddAsync(Route route, CancellationToken cancellationToken = default);
    Task UpdateAsync(Route route, CancellationToken cancellationToken = default);
    Task DeleteAsync(Route route, CancellationToken cancellationToken = default);
}
