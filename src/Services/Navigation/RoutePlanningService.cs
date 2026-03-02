namespace GalacticTrader.Services.Navigation;

using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using Microsoft.Extensions.Logging;

public sealed class RoutePlanningService : IRoutePlanningService
{
    private static readonly IReadOnlyDictionary<TravelMode, TravelModeProfile> ModeProfiles =
        new Dictionary<TravelMode, TravelModeProfile>
        {
            [TravelMode.Standard] = new()
            {
                Mode = TravelMode.Standard,
                TimeMultiplier = 1.00,
                FuelMultiplier = 1.00,
                RiskMultiplier = 1.00
            },
            [TravelMode.HighBurn] = new()
            {
                Mode = TravelMode.HighBurn,
                TimeMultiplier = 0.65,
                FuelMultiplier = 1.60,
                RiskMultiplier = 1.25
            },
            [TravelMode.StealthTransit] = new()
            {
                Mode = TravelMode.StealthTransit,
                TimeMultiplier = 1.25,
                FuelMultiplier = 1.20,
                RiskMultiplier = 0.55
            },
            [TravelMode.Convoy] = new()
            {
                Mode = TravelMode.Convoy,
                TimeMultiplier = 1.35,
                FuelMultiplier = 1.05,
                RiskMultiplier = 0.80
            },
            [TravelMode.GhostRoute] = new()
            {
                Mode = TravelMode.GhostRoute,
                TimeMultiplier = 1.70,
                FuelMultiplier = 0.90,
                RiskMultiplier = 0.35
            },
            [TravelMode.ArmedEscort] = new()
            {
                Mode = TravelMode.ArmedEscort,
                TimeMultiplier = 1.15,
                FuelMultiplier = 1.30,
                RiskMultiplier = 0.50
            }
        };

    private readonly IRouteRepository _routeRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<RoutePlanningService> _logger;

    public RoutePlanningService(
        IRouteRepository routeRepository,
        ISectorRepository sectorRepository,
        ICacheService cache,
        ILogger<RoutePlanningService> logger)
    {
        _routeRepository = routeRepository;
        _sectorRepository = sectorRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RoutePlanDto?> CalculateRouteAsync(
        Guid fromSectorId,
        Guid toSectorId,
        TravelMode travelMode = TravelMode.Standard,
        string algorithm = "dijkstra",
        CancellationToken cancellationToken = default)
    {
        if (fromSectorId == Guid.Empty || toSectorId == Guid.Empty)
        {
            throw new InvalidOperationException("Both from and to sector identifiers are required.");
        }

        if (fromSectorId == toSectorId)
        {
            return new RoutePlanDto
            {
                FromSectorId = fromSectorId,
                ToSectorId = toSectorId,
                Algorithm = NormalizeAlgorithm(algorithm),
                TravelMode = travelMode,
                TotalCost = 0,
                TotalTravelTimeSeconds = 0,
                TotalFuelCost = 0,
                TotalRiskScore = 0,
                SectorPath = [fromSectorId],
                Hops = []
            };
        }

        var normalizedAlgorithm = NormalizeAlgorithm(algorithm);
        var cacheKey = string.Format(
            CacheKeys.ROUTE_PLAN,
            normalizedAlgorithm,
            travelMode.ToString().ToLowerInvariant(),
            fromSectorId.ToString("N"),
            toSectorId.ToString("N"));

        var cachedPlan = await _cache.GetAsync<RoutePlanDto>(cacheKey);
        if (cachedPlan is not null)
        {
            return cachedPlan;
        }

        var sectorsTask = _sectorRepository.GetAllAsync(cancellationToken);
        var routesTask = _routeRepository.GetAllAsync(cancellationToken);
        await Task.WhenAll(sectorsTask, routesTask);

        var sectors = sectorsTask.Result;
        var routes = routesTask.Result;
        var profile = ModeProfiles[travelMode];

        var sectorsById = sectors.ToDictionary(sector => sector.Id);
        if (!sectorsById.ContainsKey(fromSectorId) || !sectorsById.ContainsKey(toSectorId))
        {
            return null;
        }

        var routesById = routes.ToDictionary(route => route.Id);
        var pathResult = normalizedAlgorithm == "astar"
            ? RunAStar(fromSectorId, toSectorId, sectorsById, routes, profile)
            : RunDijkstra(fromSectorId, toSectorId, routes, profile);

        if (pathResult is null)
        {
            return null;
        }

        var plan = BuildRoutePlan(
            fromSectorId,
            toSectorId,
            normalizedAlgorithm,
            profile,
            pathResult,
            routesById,
            sectorsById);

        await _cache.SetAsync(cacheKey, plan, TimeSpan.FromMinutes(15));
        _logger.LogInformation(
            "Calculated route plan from {FromSectorId} to {ToSectorId} using {Algorithm} in {Mode} mode.",
            fromSectorId,
            toSectorId,
            normalizedAlgorithm,
            travelMode);

        return plan;
    }

    public async Task<RouteOptimizationDto> GetOptimizedRoutesAsync(
        Guid fromSectorId,
        Guid toSectorId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(
            CacheKeys.ROUTE_OPTIMIZATION,
            fromSectorId.ToString("N"),
            toSectorId.ToString("N"));

        var cached = await _cache.GetAsync<RouteOptimizationDto>(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var plans = new List<RoutePlanDto>();
        foreach (var mode in Enum.GetValues<TravelMode>())
        {
            var plan = await CalculateRouteAsync(
                fromSectorId,
                toSectorId,
                mode,
                "dijkstra",
                cancellationToken);

            if (plan is not null)
            {
                plans.Add(plan);
            }
        }

        var optimization = new RouteOptimizationDto
        {
            Fastest = plans.MinBy(plan => plan.TotalTravelTimeSeconds),
            Cheapest = plans.MinBy(plan => plan.TotalFuelCost),
            Safest = plans.MinBy(plan => plan.TotalRiskScore),
            Balanced = plans.MinBy(plan => plan.TotalCost)
        };

        await _cache.SetAsync(cacheKey, optimization, TimeSpan.FromMinutes(10));
        return optimization;
    }

    private static PathResult? RunDijkstra(
        Guid fromSectorId,
        Guid toSectorId,
        IReadOnlyCollection<Route> routes,
        TravelModeProfile profile)
    {
        var adjacency = routes
            .GroupBy(route => route.FromSectorId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var distances = new Dictionary<Guid, double> { [fromSectorId] = 0d };
        var previousSector = new Dictionary<Guid, Guid>();
        var previousRoute = new Dictionary<Guid, Guid>();
        var queue = new PriorityQueue<Guid, double>();
        queue.Enqueue(fromSectorId, 0d);

        while (queue.Count > 0)
        {
            queue.TryDequeue(out var currentSectorId, out var currentPriority);

            if (currentSectorId == toSectorId)
            {
                break;
            }

            if (currentPriority > distances.GetValueOrDefault(currentSectorId, double.MaxValue))
            {
                continue;
            }

            if (!adjacency.TryGetValue(currentSectorId, out var outgoingRoutes))
            {
                continue;
            }

            foreach (var route in outgoingRoutes)
            {
                var neighbor = route.ToSectorId;
                var nextCost = distances[currentSectorId] + ComputeRouteCost(route, profile);

                if (nextCost >= distances.GetValueOrDefault(neighbor, double.MaxValue))
                {
                    continue;
                }

                distances[neighbor] = nextCost;
                previousSector[neighbor] = currentSectorId;
                previousRoute[neighbor] = route.Id;
                queue.Enqueue(neighbor, nextCost);
            }
        }

        return BuildPathResult(fromSectorId, toSectorId, distances, previousSector, previousRoute);
    }

    private static PathResult? RunAStar(
        Guid fromSectorId,
        Guid toSectorId,
        IReadOnlyDictionary<Guid, Sector> sectorsById,
        IReadOnlyCollection<Route> routes,
        TravelModeProfile profile)
    {
        var adjacency = routes
            .GroupBy(route => route.FromSectorId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var gScore = new Dictionary<Guid, double> { [fromSectorId] = 0d };
        var fScore = new Dictionary<Guid, double>
        {
            [fromSectorId] = Heuristic(fromSectorId, toSectorId, sectorsById, profile)
        };
        var previousSector = new Dictionary<Guid, Guid>();
        var previousRoute = new Dictionary<Guid, Guid>();
        var openSet = new PriorityQueue<Guid, double>();
        openSet.Enqueue(fromSectorId, fScore[fromSectorId]);

        while (openSet.Count > 0)
        {
            openSet.TryDequeue(out var currentSectorId, out _);

            if (currentSectorId == toSectorId)
            {
                break;
            }

            if (!adjacency.TryGetValue(currentSectorId, out var outgoingRoutes))
            {
                continue;
            }

            foreach (var route in outgoingRoutes)
            {
                var neighbor = route.ToSectorId;
                var tentativeGScore = gScore.GetValueOrDefault(currentSectorId, double.MaxValue) +
                    ComputeRouteCost(route, profile);

                if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, double.MaxValue))
                {
                    continue;
                }

                previousSector[neighbor] = currentSectorId;
                previousRoute[neighbor] = route.Id;
                gScore[neighbor] = tentativeGScore;

                var nextFScore = tentativeGScore + Heuristic(neighbor, toSectorId, sectorsById, profile);
                fScore[neighbor] = nextFScore;
                openSet.Enqueue(neighbor, nextFScore);
            }
        }

        return BuildPathResult(fromSectorId, toSectorId, gScore, previousSector, previousRoute);
    }

    private static PathResult? BuildPathResult(
        Guid fromSectorId,
        Guid toSectorId,
        IReadOnlyDictionary<Guid, double> distances,
        IReadOnlyDictionary<Guid, Guid> previousSector,
        IReadOnlyDictionary<Guid, Guid> previousRoute)
    {
        if (!distances.ContainsKey(toSectorId))
        {
            return null;
        }

        var sectorPath = new List<Guid> { toSectorId };
        var routePath = new List<Guid>();
        var current = toSectorId;

        while (current != fromSectorId)
        {
            if (!previousSector.TryGetValue(current, out var predecessor) ||
                !previousRoute.TryGetValue(current, out var usedRouteId))
            {
                return null;
            }

            routePath.Add(usedRouteId);
            sectorPath.Add(predecessor);
            current = predecessor;
        }

        sectorPath.Reverse();
        routePath.Reverse();

        return new PathResult
        {
            SectorPath = sectorPath,
            RoutePath = routePath,
            TotalCost = distances[toSectorId]
        };
    }

    private static double Heuristic(
        Guid fromSectorId,
        Guid toSectorId,
        IReadOnlyDictionary<Guid, Sector> sectorsById,
        TravelModeProfile profile)
    {
        var from = sectorsById[fromSectorId];
        var to = sectorsById[toSectorId];

        var dx = from.X - to.X;
        var dy = from.Y - to.Y;
        var dz = from.Z - to.Z;
        var distance = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));

        // Use a tiny admissible lower-bound heuristic to preserve optimality.
        return distance * 0.001d * profile.FuelMultiplier;
    }

    private static double ComputeRouteCost(Route route, TravelModeProfile profile)
    {
        var timeCost = route.TravelTimeSeconds * profile.TimeMultiplier;
        var fuelCost = route.FuelCost * 10d * profile.FuelMultiplier;
        var riskCost = route.BaseRiskScore * profile.RiskMultiplier;

        var legalPenalty = route.LegalStatus.Equals("Illegal", StringComparison.OrdinalIgnoreCase) ? 15d : 0d;
        var anomalyPenalty = route.HasAnomalies ? 20d : 0d;

        return timeCost + fuelCost + riskCost + legalPenalty + anomalyPenalty;
    }

    private static RoutePlanDto BuildRoutePlan(
        Guid fromSectorId,
        Guid toSectorId,
        string algorithm,
        TravelModeProfile profile,
        PathResult pathResult,
        IReadOnlyDictionary<Guid, Route> routesById,
        IReadOnlyDictionary<Guid, Sector> sectorsById)
    {
        var hops = pathResult.RoutePath
            .Select(routeId => routesById[routeId])
            .Select(route => new RouteHopDto
            {
                RouteId = route.Id,
                FromSectorId = route.FromSectorId,
                ToSectorId = route.ToSectorId,
                FromSectorName = sectorsById[route.FromSectorId].Name,
                ToSectorName = sectorsById[route.ToSectorId].Name,
                BaseTravelTimeSeconds = route.TravelTimeSeconds,
                BaseFuelCost = route.FuelCost,
                BaseRiskScore = route.BaseRiskScore
            })
            .ToList();

        var totalTravel = (int)Math.Round(hops.Sum(hop => hop.BaseTravelTimeSeconds * profile.TimeMultiplier));
        var totalFuel = hops.Sum(hop => hop.BaseFuelCost * profile.FuelMultiplier);
        var totalRisk = hops.Sum(hop => hop.BaseRiskScore * profile.RiskMultiplier);

        return new RoutePlanDto
        {
            FromSectorId = fromSectorId,
            ToSectorId = toSectorId,
            Algorithm = algorithm,
            TravelMode = profile.Mode,
            TotalCost = Math.Round(pathResult.TotalCost, 2),
            TotalTravelTimeSeconds = totalTravel,
            TotalFuelCost = Math.Round(totalFuel, 2),
            TotalRiskScore = Math.Round(totalRisk, 2),
            SectorPath = pathResult.SectorPath,
            Hops = hops
        };
    }

    private static string NormalizeAlgorithm(string algorithm)
    {
        if (algorithm.Equals("astar", StringComparison.OrdinalIgnoreCase) ||
            algorithm.Equals("a*", StringComparison.OrdinalIgnoreCase))
        {
            return "astar";
        }

        return "dijkstra";
    }

    private sealed class PathResult
    {
        public required List<Guid> SectorPath { get; init; }
        public required List<Guid> RoutePath { get; init; }
        public required double TotalCost { get; init; }
    }
}
