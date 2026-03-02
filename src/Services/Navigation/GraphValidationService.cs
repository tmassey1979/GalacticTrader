namespace GalacticTrader.Services.Navigation;

using GalacticTrader.Data.Repositories.Navigation;

public sealed class GraphValidationService : IGraphValidationService
{
    private readonly ISectorRepository _sectorRepository;
    private readonly IRouteRepository _routeRepository;

    public GraphValidationService(
        ISectorRepository sectorRepository,
        IRouteRepository routeRepository)
    {
        _sectorRepository = sectorRepository;
        _routeRepository = routeRepository;
    }

    public async Task EnsureSectorCanBeCreatedAsync(string sectorName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sectorName))
        {
            throw new InvalidOperationException("Sector name is required.");
        }

        var trimmedName = sectorName.Trim();
        var exists = await _sectorRepository.ExistsByNameAsync(trimmedName, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Sector with name '{trimmedName}' already exists.");
        }
    }

    public async Task EnsureRouteCanBeCreatedAsync(
        Guid fromSectorId,
        Guid toSectorId,
        CancellationToken cancellationToken = default)
    {
        if (fromSectorId == Guid.Empty || toSectorId == Guid.Empty)
        {
            throw new InvalidOperationException("Both sector identifiers are required.");
        }

        if (fromSectorId == toSectorId)
        {
            throw new InvalidOperationException("Routes cannot start and end in the same sector.");
        }

        var fromSector = await _sectorRepository.GetByIdAsync(fromSectorId, cancellationToken);
        var toSector = await _sectorRepository.GetByIdAsync(toSectorId, cancellationToken);
        if (fromSector is null || toSector is null)
        {
            throw new InvalidOperationException("One or both sectors do not exist.");
        }

        var routeExists = await _routeRepository.ExistsAsync(fromSectorId, toSectorId, cancellationToken);
        if (routeExists)
        {
            throw new InvalidOperationException("A route between these sectors already exists.");
        }
    }

    public async Task<GraphValidationReport> ValidateGraphAsync(CancellationToken cancellationToken = default)
    {
        var sectors = await _sectorRepository.GetAllAsync(cancellationToken);
        var routes = await _routeRepository.GetAllAsync(cancellationToken);

        var report = new GraphValidationReport
        {
            SectorCount = sectors.Count,
            RouteCount = routes.Count
        };

        var sectorIdSet = sectors.Select(sector => sector.Id).ToHashSet();

        foreach (var route in routes)
        {
            if (!sectorIdSet.Contains(route.FromSectorId) || !sectorIdSet.Contains(route.ToSectorId))
            {
                report.Errors.Add($"Route '{route.Id}' references a sector that does not exist.");
            }

            if (route.FromSectorId == route.ToSectorId)
            {
                report.Errors.Add($"Route '{route.Id}' is a self-loop.");
            }
        }

        var duplicateDirectedEdges = routes
            .GroupBy(route => $"{route.FromSectorId:N}:{route.ToSectorId:N}")
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var edge in duplicateDirectedEdges)
        {
            report.Errors.Add($"Duplicate directed edge detected for '{edge}'.");
        }

        if (sectors.Count > 0)
        {
            var connectedSectorIds = routes
                .SelectMany(route => new[] { route.FromSectorId, route.ToSectorId })
                .ToHashSet();

            var isolatedSectors = sectors
                .Where(sector => !connectedSectorIds.Contains(sector.Id))
                .Select(sector => sector.Name)
                .OrderBy(name => name)
                .ToList();

            foreach (var isolatedSector in isolatedSectors)
            {
                report.Warnings.Add($"Isolated sector with no routes: '{isolatedSector}'.");
            }
        }

        return report;
    }
}
