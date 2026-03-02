namespace GalacticTrader.Data.Repositories.Navigation;

using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;

public sealed class RouteRepository : IRouteRepository
{
    private readonly GalacticTraderDbContext _dbContext;

    public RouteRepository(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Route>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes
            .Include(route => route.FromSector)
            .Include(route => route.ToSector)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Route?> GetByIdAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes
            .Include(route => route.FromSector)
            .Include(route => route.ToSector)
            .FirstOrDefaultAsync(route => route.Id == routeId, cancellationToken);
    }

    public Task<List<Route>> GetBySectorIdAsync(Guid sectorId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes
            .Include(route => route.FromSector)
            .Include(route => route.ToSector)
            .Where(route => route.FromSectorId == sectorId || route.ToSectorId == sectorId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<Route>> GetOutboundAsync(Guid fromSectorId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes
            .Include(route => route.FromSector)
            .Include(route => route.ToSector)
            .Where(route => route.FromSectorId == fromSectorId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<Route>> GetInboundAsync(Guid toSectorId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes
            .Include(route => route.FromSector)
            .Include(route => route.ToSector)
            .Where(route => route.ToSectorId == toSectorId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<Route>> GetBetweenAsync(
        Guid sectorAId,
        Guid sectorBId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes
            .Include(route => route.FromSector)
            .Include(route => route.ToSector)
            .Where(route =>
                (route.FromSectorId == sectorAId && route.ToSectorId == sectorBId) ||
                (route.FromSectorId == sectorBId && route.ToSectorId == sectorAId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid fromSectorId, Guid toSectorId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Routes.AnyAsync(
            route => route.FromSectorId == fromSectorId && route.ToSectorId == toSectorId,
            cancellationToken);
    }

    public async Task AddAsync(Route route, CancellationToken cancellationToken = default)
    {
        _dbContext.Routes.Add(route);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Route route, CancellationToken cancellationToken = default)
    {
        _dbContext.Routes.Update(route);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Route route, CancellationToken cancellationToken = default)
    {
        _dbContext.Routes.Remove(route);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
