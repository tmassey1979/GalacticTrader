namespace GalacticTrader.Data.Repositories.Navigation;

using GalacticTrader.Data.Models;

public interface ISectorRepository
{
    Task<List<Sector>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Sector?> GetByIdAsync(Guid sectorId, CancellationToken cancellationToken = default);
    Task<Sector?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Sector>> GetByIdsAsync(IEnumerable<Guid> sectorIds, CancellationToken cancellationToken = default);
    Task<List<Sector>> GetByCoordinatesRangeAsync(
        float minX,
        float maxX,
        float minY,
        float maxY,
        float minZ,
        float maxZ,
        CancellationToken cancellationToken = default);
    Task<List<Sector>> GetByFactionAsync(Guid factionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Sector sector, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sector sector, CancellationToken cancellationToken = default);
    Task DeleteAsync(Sector sector, CancellationToken cancellationToken = default);
}
