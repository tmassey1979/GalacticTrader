namespace GalacticTrader.Services.Navigation;

using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using Microsoft.Extensions.Logging;

public sealed class RouteService : IRouteService
{
    private readonly IRouteRepository _routeRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IGraphValidationService _graphValidationService;
    private readonly ICacheService _cache;
    private readonly ILogger<RouteService> _logger;

    public RouteService(
        IRouteRepository routeRepository,
        ISectorRepository sectorRepository,
        IGraphValidationService graphValidationService,
        ICacheService cache,
        ILogger<RouteService> logger)
    {
        _routeRepository = routeRepository;
        _sectorRepository = sectorRepository;
        _graphValidationService = graphValidationService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<RouteDto>> GetAllRoutesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = CacheKeys.ROUTE_CACHE_PREFIX + "all";
        var cached = await _cache.GetAsync<List<RouteDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var routes = await _routeRepository.GetAllAsync(cancellationToken);
        var result = routes.Select(MapToDto).ToList();
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

        return result;
    }

    public async Task<RouteDto?> GetRouteByIdAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(routeId, cancellationToken);
        return route is null ? null : MapToDto(route);
    }

    public async Task<IEnumerable<RouteDto>> GetOutboundRoutesAsync(Guid fromSectorId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeys.ROUTE_CACHE_PREFIX}outbound:{fromSectorId}";
        var cached = await _cache.GetAsync<List<RouteDto>>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var routes = await _routeRepository.GetOutboundAsync(fromSectorId, cancellationToken);
        var result = routes.Select(MapToDto).ToList();
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1));

        return result;
    }

    public async Task<IEnumerable<RouteDto>> GetInboundRoutesAsync(Guid toSectorId, CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetInboundAsync(toSectorId, cancellationToken);
        return routes.Select(MapToDto);
    }

    public async Task<IEnumerable<RouteDto>> GetRoutesBetweenAsync(
        Guid sectorAId,
        Guid sectorBId,
        CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetBetweenAsync(sectorAId, sectorBId, cancellationToken);
        return routes.Select(MapToDto);
    }

    public async Task<RouteDto> CreateRouteAsync(
        Guid fromSectorId,
        Guid toSectorId,
        string legalStatus,
        string warpGateType,
        CancellationToken cancellationToken = default)
    {
        await _graphValidationService.EnsureRouteCanBeCreatedAsync(fromSectorId, toSectorId, cancellationToken);

        var fromSector = await _sectorRepository.GetByIdAsync(fromSectorId, cancellationToken)
            ?? throw new InvalidOperationException($"From sector '{fromSectorId}' was not found.");
        var toSector = await _sectorRepository.GetByIdAsync(toSectorId, cancellationToken)
            ?? throw new InvalidOperationException($"To sector '{toSectorId}' was not found.");

        var distance = CalculateDistance(fromSector, toSector);
        var baseRiskScore = legalStatus.Equals("Illegal", StringComparison.OrdinalIgnoreCase) ? 80f : 20f;

        var route = new Route
        {
            Id = Guid.NewGuid(),
            FromSectorId = fromSectorId,
            ToSectorId = toSectorId,
            TravelTimeSeconds = Math.Max(1, (int)Math.Round(distance * 60)),
            FuelCost = Math.Max(0.01f, (float)Math.Round(distance * 0.1d, 2)),
            BaseRiskScore = baseRiskScore,
            VisibilityRating = Math.Clamp(100f - baseRiskScore, 0f, 100f),
            LegalStatus = legalStatus.Trim(),
            WarpGateType = warpGateType.Trim(),
            IsDiscovered = true,
            HasAnomalies = false,
            TrafficIntensity = 50
        };

        await _routeRepository.AddAsync(route, cancellationToken);
        await InvalidateRouteCachesAsync(fromSectorId, toSectorId);

        _logger.LogInformation(
            "Route created: {RouteId} ({FromSector} -> {ToSector})",
            route.Id,
            fromSector.Name,
            toSector.Name);

        var created = await _routeRepository.GetByIdAsync(route.Id, cancellationToken) ?? route;
        return MapToDto(created);
    }

    public async Task<RouteDto?> UpdateRouteAsync(
        Guid routeId,
        string? legalStatus = null,
        float? baseRiskScore = null,
        CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(routeId, cancellationToken);
        if (route is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(legalStatus))
        {
            route.LegalStatus = legalStatus.Trim();
        }

        if (baseRiskScore.HasValue)
        {
            route.BaseRiskScore = Math.Clamp(baseRiskScore.Value, 0f, 100f);
            route.VisibilityRating = Math.Clamp(100f - route.BaseRiskScore, 0f, 100f);
        }

        await _routeRepository.UpdateAsync(route, cancellationToken);
        await InvalidateRouteCachesAsync(route.FromSectorId, route.ToSectorId);

        _logger.LogInformation("Route updated: {RouteId}", routeId);

        var updated = await _routeRepository.GetByIdAsync(routeId, cancellationToken) ?? route;
        return MapToDto(updated);
    }

    public async Task<bool> DeleteRouteAsync(Guid routeId, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetByIdAsync(routeId, cancellationToken);
        if (route is null)
        {
            return false;
        }

        await _routeRepository.DeleteAsync(route, cancellationToken);
        await InvalidateRouteCachesAsync(route.FromSectorId, route.ToSectorId);

        _logger.LogInformation("Route deleted: {RouteId}", routeId);

        return true;
    }

    public async Task<double?> GetDistanceBetweenSectorsAsync(
        Guid sectorAId,
        Guid sectorBId,
        CancellationToken cancellationToken = default)
    {
        var sectorA = await _sectorRepository.GetByIdAsync(sectorAId, cancellationToken);
        var sectorB = await _sectorRepository.GetByIdAsync(sectorBId, cancellationToken);
        if (sectorA is null || sectorB is null)
        {
            return null;
        }

        return CalculateDistance(sectorA, sectorB);
    }

    public async Task<IEnumerable<RouteDto>> GetDangerousRoutesAsync(int riskThreshold = 70, CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetAllAsync(cancellationToken);
        return routes
            .Where(route => route.BaseRiskScore >= riskThreshold)
            .OrderByDescending(route => route.BaseRiskScore)
            .Select(MapToDto);
    }

    public async Task<IEnumerable<RouteDto>> GetLegalRoutesAsync(CancellationToken cancellationToken = default)
    {
        var routes = await _routeRepository.GetAllAsync(cancellationToken);
        return routes
            .Where(route => route.LegalStatus.Equals("Legal", StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto);
    }

    private async Task InvalidateRouteCachesAsync(Guid fromSectorId, Guid toSectorId)
    {
        await _cache.RemoveByPatternAsync($"{CacheKeys.ROUTE_CACHE_PREFIX}*");
        await _cache.RemoveAsync(string.Format(CacheKeys.ROUTE_DETAILS, fromSectorId, toSectorId));
        await _cache.RemoveAsync(string.Format(CacheKeys.ROUTE_DETAILS, toSectorId, fromSectorId));
        await _cache.RemoveAsync(CacheKeys.SECTOR_GRAPH);
    }

    private static double CalculateDistance(Sector from, Sector to)
    {
        var dx = from.X - to.X;
        var dy = from.Y - to.Y;
        var dz = from.Z - to.Z;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    private static RouteDto MapToDto(Route route)
    {
        return new RouteDto
        {
            Id = route.Id,
            FromSectorId = route.FromSectorId,
            ToSectorId = route.ToSectorId,
            FromSectorName = route.FromSector?.Name ?? "Unknown",
            ToSectorName = route.ToSector?.Name ?? "Unknown",
            TravelTimeSeconds = route.TravelTimeSeconds,
            FuelCost = route.FuelCost,
            BaseRiskScore = route.BaseRiskScore,
            VisibilityRating = route.VisibilityRating,
            LegalStatus = route.LegalStatus,
            WarpGateType = route.WarpGateType,
            IsDiscovered = route.IsDiscovered,
            HasAnomalies = route.HasAnomalies,
            TrafficIntensity = route.TrafficIntensity
        };
    }
}
