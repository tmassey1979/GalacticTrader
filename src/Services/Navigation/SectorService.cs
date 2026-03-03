namespace GalacticTrader.Services.Navigation;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class SectorService : ISectorService
{
    private readonly GalacticTraderDbContext _dbContext;
    private readonly ISectorRepository _sectorRepository;
    private readonly IRouteRepository _routeRepository;
    private readonly IGraphValidationService _graphValidationService;
    private readonly ICacheService _cache;
    private readonly ILogger<SectorService> _logger;

    public SectorService(
        GalacticTraderDbContext dbContext,
        ISectorRepository sectorRepository,
        IRouteRepository routeRepository,
        IGraphValidationService graphValidationService,
        ICacheService cache,
        ILogger<SectorService> logger)
    {
        _dbContext = dbContext;
        _sectorRepository = sectorRepository;
        _routeRepository = routeRepository;
        _graphValidationService = graphValidationService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<SectorDto>> GetAllSectorsAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<SectorDto>>(CacheKeys.SECTOR_LIST_ALL);
        if (cached is not null)
        {
            return cached;
        }

        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);
        var result = sectors.Select(MapToDto).ToList();
        await _cache.SetAsync(CacheKeys.SECTOR_LIST_ALL, result, TimeSpan.FromHours(1));

        return result;
    }

    public async Task<SectorDto?> GetSectorByIdAsync(Guid sectorId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(CacheKeys.SECTOR_DATA, sectorId);
        var cached = await _cache.GetAsync<SectorDto>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var sector = await _sectorRepository.GetByIdAsync(sectorId, cancellationToken);
        if (sector is null)
        {
            return null;
        }

        var dto = MapToDto(sector);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1));
        return dto;
    }

    public async Task<SectorDto?> GetSectorByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var sector = await _sectorRepository.GetByNameAsync(name, cancellationToken);
        return sector is null ? null : MapToDto(sector);
    }

    public async Task<IEnumerable<SectorDto>> GetSectorsByCoordsRangeAsync(
        float minX,
        float maxX,
        float minY,
        float maxY,
        float minZ,
        float maxZ,
        CancellationToken cancellationToken = default)
    {
        var sectors = await _sectorRepository.GetByCoordinatesRangeAsync(
            minX,
            maxX,
            minY,
            maxY,
            minZ,
            maxZ,
            cancellationToken);
        return sectors.Select(MapToDto);
    }

    public async Task<IEnumerable<SectorDto>> GetSectorsByFactionAsync(Guid factionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeys.SECTOR_PREFIX}faction:{factionId}";
        var cached = await _cache.GetAsync<List<SectorDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var sectors = await _sectorRepository.GetByFactionAsync(factionId, cancellationToken);
        var dtos = sectors.Select(MapToDto).ToList();
        await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(30));

        return dtos;
    }

    public async Task<IEnumerable<SectorDto>> GetAdjacentSectorsAsync(Guid sectorId, CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetBySectorIdAsync(sectorId, cancellationToken);
        var adjacentIds = routes
            .Select(route => route.FromSectorId == sectorId ? route.ToSectorId : route.FromSectorId)
            .Distinct()
            .ToHashSet();

        if (adjacentIds.Count == 0)
        {
            return [];
        }

        var sectors = await _sectorRepository.GetByIdsAsync(adjacentIds, cancellationToken);
        return sectors.Select(MapToDto);
    }

    public async Task<SectorDto> CreateSectorAsync(
        string name,
        float x,
        float y,
        float z,
        CancellationToken cancellationToken = default)
    {
        await _graphValidationService.EnsureSectorCanBeCreatedAsync(name, cancellationToken);

        var sector = new Sector
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            X = x,
            Y = y,
            Z = z,
            SecurityLevel = 50,
            HazardRating = 0,
            ResourceModifier = 1.0f,
            EconomicIndex = 50,
            SensorInterferenceLevel = 0f,
            AverageTrafficLevel = 50,
            PiratePresenceProbability = 10
        };

        await _sectorRepository.AddAsync(sector, cancellationToken);
        await InvalidateSectorCachesAsync(sector.Id);

        _logger.LogInformation("Sector created: {SectorId} ({SectorName})", sector.Id, sector.Name);

        var created = await _sectorRepository.GetByIdAsync(sector.Id, cancellationToken) ?? sector;
        return MapToDto(created);
    }

    public async Task<SectorDto?> UpdateSectorAsync(
        Guid sectorId,
        int? securityLevel = null,
        int? hazardRating = null,
        Guid? factionId = null,
        CancellationToken cancellationToken = default)
    {
        var sector = await _sectorRepository.GetByIdAsync(sectorId, cancellationToken);
        if (sector is null)
        {
            return null;
        }

        if (securityLevel.HasValue)
        {
            sector.SecurityLevel = Math.Clamp(securityLevel.Value, 0, 100);
        }

        if (hazardRating.HasValue)
        {
            sector.HazardRating = Math.Clamp(hazardRating.Value, 0, 100);
        }

        if (factionId.HasValue)
        {
            sector.ControlledByFactionId = factionId.Value;
        }

        await _sectorRepository.UpdateAsync(sector, cancellationToken);
        await InvalidateSectorCachesAsync(sectorId);

        _logger.LogInformation("Sector updated: {SectorId}", sectorId);

        var updated = await _sectorRepository.GetByIdAsync(sectorId, cancellationToken) ?? sector;
        return MapToDto(updated);
    }

    public async Task<bool> DeleteSectorAsync(Guid sectorId, CancellationToken cancellationToken = default)
    {
        var sector = await _sectorRepository.GetByIdAsync(sectorId, cancellationToken);
        if (sector is null)
        {
            return false;
        }

        var shipsToUpdate = await _dbContext.Ships
            .Where(ship => ship.CurrentSectorId == sectorId || ship.TargetSectorId == sectorId)
            .ToListAsync(cancellationToken);
        foreach (var ship in shipsToUpdate)
        {
            if (ship.CurrentSectorId == sectorId)
            {
                ship.CurrentSectorId = null;
            }

            if (ship.TargetSectorId == sectorId)
            {
                ship.TargetSectorId = null;
            }
        }

        var npcAgentsToUpdate = await _dbContext.NPCAgents
            .Where(agent => agent.CurrentLocationId == sectorId || agent.TargetLocationId == sectorId)
            .ToListAsync(cancellationToken);
        foreach (var agent in npcAgentsToUpdate)
        {
            if (agent.CurrentLocationId == sectorId)
            {
                agent.CurrentLocationId = null;
            }

            if (agent.TargetLocationId == sectorId)
            {
                agent.TargetLocationId = null;
            }
        }

        var npcShipsToUpdate = await _dbContext.Set<NPCShip>()
            .Where(ship => ship.CurrentSectorId == sectorId)
            .ToListAsync(cancellationToken);
        foreach (var npcShip in npcShipsToUpdate)
        {
            npcShip.CurrentSectorId = null;
        }

        if (shipsToUpdate.Count > 0 || npcAgentsToUpdate.Count > 0 || npcShipsToUpdate.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var attachedRoutes = await _routeRepository.GetBySectorIdAsync(sectorId, cancellationToken);
        foreach (var route in attachedRoutes)
        {
            await _routeRepository.DeleteAsync(route, cancellationToken);
        }

        await _sectorRepository.DeleteAsync(sector, cancellationToken);
        await InvalidateSectorCachesAsync(sectorId);

        _logger.LogInformation("Sector deleted: {SectorId}", sectorId);

        return true;
    }

    public async Task<IEnumerable<SectorDto>> GetHighSecuritySectorsAsync(int threshold = 70, CancellationToken cancellationToken = default)
    {
        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);
        return sectors
            .Where(sector => sector.SecurityLevel >= threshold)
            .OrderByDescending(sector => sector.SecurityLevel)
            .Select(MapToDto);
    }

    public async Task<IEnumerable<SectorDto>> GetHighRiskSectorsAsync(int threshold = 70, CancellationToken cancellationToken = default)
    {
        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);
        return sectors
            .Where(sector => sector.HazardRating >= threshold)
            .OrderByDescending(sector => sector.HazardRating)
            .Select(MapToDto);
    }

    public async Task<Dictionary<string, int>> GetSecurityLevelDistributionAsync(CancellationToken cancellationToken = default)
    {
        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);

        return new Dictionary<string, int>
        {
            { "VeryHigh (80-100)", sectors.Count(sector => sector.SecurityLevel >= 80) },
            { "High (60-79)", sectors.Count(sector => sector.SecurityLevel is >= 60 and < 80) },
            { "Medium (40-59)", sectors.Count(sector => sector.SecurityLevel is >= 40 and < 60) },
            { "Low (20-39)", sectors.Count(sector => sector.SecurityLevel is >= 20 and < 40) },
            { "VeryLow (0-19)", sectors.Count(sector => sector.SecurityLevel < 20) }
        };
    }

    private async Task InvalidateSectorCachesAsync(Guid sectorId)
    {
        await _cache.RemoveAsync(string.Format(CacheKeys.SECTOR_DATA, sectorId));
        await _cache.RemoveAsync(CacheKeys.SECTOR_LIST_ALL);
        await _cache.RemoveByPatternAsync($"{CacheKeys.SECTOR_PREFIX}*");
    }

    private static SectorDto MapToDto(Sector sector)
    {
        return new SectorDto
        {
            Id = sector.Id,
            Name = sector.Name,
            X = sector.X,
            Y = sector.Y,
            Z = sector.Z,
            SecurityLevel = sector.SecurityLevel,
            HazardRating = sector.HazardRating,
            ResourceModifier = sector.ResourceModifier,
            EconomicIndex = sector.EconomicIndex,
            SensorInterferenceLevel = sector.SensorInterferenceLevel,
            ControlledByFactionId = sector.ControlledByFactionId,
            ControlledByFactionName = sector.ControlledByFaction?.Name,
            ConnectedSectorCount = sector.OutboundRoutes.Count + sector.InboundRoutes.Count
        };
    }
}
