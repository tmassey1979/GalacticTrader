namespace GalacticTrader.Data.Repositories.Navigation;

using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;

public sealed class SectorRepository : ISectorRepository
{
    private readonly GalacticTraderDbContext _dbContext;

    public SectorRepository(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Sector>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors
            .Include(sector => sector.ControlledByFaction)
            .Include(sector => sector.OutboundRoutes)
            .Include(sector => sector.InboundRoutes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Sector?> GetByIdAsync(Guid sectorId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors
            .Include(sector => sector.ControlledByFaction)
            .Include(sector => sector.OutboundRoutes)
            .Include(sector => sector.InboundRoutes)
            .FirstOrDefaultAsync(sector => sector.Id == sectorId, cancellationToken);
    }

    public Task<Sector?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors
            .Include(sector => sector.ControlledByFaction)
            .Include(sector => sector.OutboundRoutes)
            .Include(sector => sector.InboundRoutes)
            .FirstOrDefaultAsync(sector => sector.Name == name, cancellationToken);
    }

    public Task<List<Sector>> GetByIdsAsync(IEnumerable<Guid> sectorIds, CancellationToken cancellationToken = default)
    {
        var idSet = sectorIds.ToHashSet();
        return _dbContext.Sectors
            .Include(sector => sector.ControlledByFaction)
            .Include(sector => sector.OutboundRoutes)
            .Include(sector => sector.InboundRoutes)
            .Where(sector => idSet.Contains(sector.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<Sector>> GetByCoordinatesRangeAsync(
        float minX,
        float maxX,
        float minY,
        float maxY,
        float minZ,
        float maxZ,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors
            .Include(sector => sector.ControlledByFaction)
            .Include(sector => sector.OutboundRoutes)
            .Include(sector => sector.InboundRoutes)
            .Where(sector =>
                sector.X >= minX &&
                sector.X <= maxX &&
                sector.Y >= minY &&
                sector.Y <= maxY &&
                sector.Z >= minZ &&
                sector.Z <= maxZ)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<Sector>> GetByFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors
            .Include(sector => sector.ControlledByFaction)
            .Include(sector => sector.OutboundRoutes)
            .Include(sector => sector.InboundRoutes)
            .Where(sector => sector.ControlledByFactionId == factionId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sectors.AnyAsync(sector => sector.Name == name, cancellationToken);
    }

    public async Task AddAsync(Sector sector, CancellationToken cancellationToken = default)
    {
        _dbContext.Sectors.Add(sector);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Sector sector, CancellationToken cancellationToken = default)
    {
        _dbContext.Sectors.Update(sector);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Sector sector, CancellationToken cancellationToken = default)
    {
        _dbContext.Sectors.Remove(sector);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
