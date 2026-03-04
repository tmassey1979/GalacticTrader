using System.Text.RegularExpressions;
using GalacticTrader.ClientSdk.Starmap;
using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Routes;

public sealed class RouteModuleService
{
    private static readonly Regex WaypointDelimiter = new(@"\s*(?:->|,|;|\||\r?\n)\s*", RegexOptions.Compiled);
    private readonly RouteDataSource _dataSource;

    public RouteModuleService(RouteDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<RouteModuleState> LoadStateAsync(
        int dangerousRiskThreshold = 70,
        CancellationToken cancellationToken = default)
    {
        var normalizedThreshold = Math.Clamp(dangerousRiskThreshold, 0, 100);
        var sectorsTask = _dataSource.LoadSectorsAsync(cancellationToken);
        var routesTask = _dataSource.LoadRoutesAsync(cancellationToken);
        var dangerousRoutesTask = _dataSource.LoadDangerousRoutesAsync(normalizedThreshold, cancellationToken);

        await Task.WhenAll(sectorsTask, routesTask, dangerousRoutesTask);

        var sectors = await sectorsTask;
        var routes = await routesTask;
        var dangerousRoutes = await dangerousRoutesTask;
        var sectorOptions = sectors
            .Select(static sector => new RouteSectorOption(sector.Id, sector.Name))
            .OrderBy(static sector => sector.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var overlay = new RouteOverlayState(
            PlannedEdges: [],
            DangerousEdges: ToOverlayEdges(dangerousRoutes, normalizedThreshold),
            SuggestedEdges: []);

        return new RouteModuleState(
            sectors,
            routes,
            dangerousRoutes,
            sectorOptions,
            overlay,
            DateTime.UtcNow);
    }

    public RouteWaypointParseResult ParseWaypoints(
        string? waypointInput,
        IReadOnlyList<RouteSectorOption> sectors)
    {
        if (string.IsNullOrWhiteSpace(waypointInput))
        {
            return RouteWaypointParseResult.Empty;
        }

        var matched = new List<RouteSectorOption>();
        var unmatchedTokens = new List<string>();
        var seen = new HashSet<Guid>();

        foreach (var token in SplitWaypointTokens(waypointInput))
        {
            var sector = TryResolveSectorToken(token, sectors);
            if (sector is null)
            {
                unmatchedTokens.Add(token);
                continue;
            }

            if (seen.Add(sector.SectorId))
            {
                matched.Add(sector);
            }
        }

        return new RouteWaypointParseResult(matched, unmatchedTokens);
    }

    public async Task<RoutePlanningResult> PlanRouteAsync(
        RouteModuleState state,
        string fromSectorToken,
        string toSectorToken,
        string? waypointInput,
        RouteTravelModePreset travelModePreset = RouteTravelModePreset.Standard,
        string algorithm = "dijkstra",
        int highRiskThreshold = 70,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var fromSector = ResolveSectorOrThrow(fromSectorToken, state.SectorOptions, "from");
        var toSector = ResolveSectorOrThrow(toSectorToken, state.SectorOptions, "to");

        if (fromSector.SectorId == toSector.SectorId)
        {
            throw new InvalidOperationException("Route start and destination cannot be the same sector.");
        }

        var waypointParse = ParseWaypoints(waypointInput, state.SectorOptions);
        if (waypointParse.UnmatchedTokens.Count > 0)
        {
            var unknown = string.Join(", ", waypointParse.UnmatchedTokens);
            throw new InvalidOperationException($"Unknown waypoint token(s): {unknown}");
        }

        var segmentChain = BuildSegmentChain(fromSector.SectorId, toSector.SectorId, waypointParse.Waypoints);
        var travelMode = ResolveTravelMode(travelModePreset);
        var normalizedAlgorithm = NormalizeAlgorithm(algorithm);
        var segmentPlans = new List<RoutePlanApiDto>(Math.Max(1, segmentChain.Count - 1));

        for (var index = 0; index < segmentChain.Count - 1; index++)
        {
            var fromSectorId = segmentChain[index];
            var toSectorId = segmentChain[index + 1];
            var segmentPlan = await _dataSource.LoadRoutePlanAsync(
                fromSectorId,
                toSectorId,
                travelMode,
                normalizedAlgorithm,
                cancellationToken);

            if (segmentPlan is null)
            {
                throw new InvalidOperationException(
                    $"No route found between '{ResolveSectorName(fromSectorId, state.SectorOptions)}' and '{ResolveSectorName(toSectorId, state.SectorOptions)}'.");
            }

            segmentPlans.Add(segmentPlan);
        }

        var mergedPlan = MergeSegments(segmentPlans, normalizedAlgorithm);
        var normalizedRiskThreshold = Math.Clamp(highRiskThreshold, 0, 100);
        var riskSimulation = SimulateRisk(
            mergedPlan,
            state.Routes,
            travelModePreset,
            normalizedRiskThreshold);
        var overlay = new RouteOverlayState(
            PlannedEdges: ToOverlayEdges(mergedPlan.Hops, normalizedRiskThreshold),
            DangerousEdges: ToOverlayEdges(state.Routes.Where(route => route.BaseRiskScore >= normalizedRiskThreshold), normalizedRiskThreshold),
            SuggestedEdges: []);

        return new RoutePlanningResult(mergedPlan, waypointParse, riskSimulation, overlay);
    }

    public async Task<RouteOptimizationView> LoadOptimizationAsync(
        RouteModuleState state,
        string fromSectorToken,
        string toSectorToken,
        int highRiskThreshold = 70,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var fromSector = ResolveSectorOrThrow(fromSectorToken, state.SectorOptions, "from");
        var toSector = ResolveSectorOrThrow(toSectorToken, state.SectorOptions, "to");

        if (fromSector.SectorId == toSector.SectorId)
        {
            throw new InvalidOperationException("Route start and destination cannot be the same sector.");
        }

        var optimization = await _dataSource.LoadRouteOptimizationAsync(
            fromSector.SectorId,
            toSector.SectorId,
            cancellationToken);

        var recommendedProfile = SelectRecommendedProfile(optimization);
        var recommendedPlan = GetPlanForProfile(optimization, recommendedProfile);
        var normalizedRiskThreshold = Math.Clamp(highRiskThreshold, 0, 100);
        var riskSimulation = recommendedPlan is null
            ? null
            : SimulateRisk(recommendedPlan, state.Routes, RouteTravelModePreset.Standard, normalizedRiskThreshold);
        var overlay = new RouteOverlayState(
            PlannedEdges: [],
            DangerousEdges: ToOverlayEdges(state.Routes.Where(route => route.BaseRiskScore >= normalizedRiskThreshold), normalizedRiskThreshold),
            SuggestedEdges: ToOverlayEdges(recommendedPlan?.Hops ?? [], normalizedRiskThreshold));

        return new RouteOptimizationView(
            optimization,
            recommendedProfile,
            riskSimulation,
            overlay);
    }

    internal static RouteRiskSimulation SimulateRisk(
        RoutePlanApiDto plan,
        IReadOnlyList<RouteApiDto> allRoutes,
        RouteTravelModePreset travelModePreset,
        int highRiskThreshold)
    {
        var hopCount = Math.Max(1, plan.Hops.Count);
        var baseRisk = plan.Hops.Count == 0
            ? Math.Clamp(plan.TotalRiskScore, 0d, 100d)
            : Math.Clamp(plan.Hops.Average(static hop => hop.BaseRiskScore), 0d, 100d);
        var piratePressure = plan.Hops.Count == 0
            ? 0d
            : plan.Hops.Count(hop => hop.BaseRiskScore >= highRiskThreshold) / (double)plan.Hops.Count;
        var economicDensity = EstimateEconomicDensity(plan.SectorPath, allRoutes);
        var modeRiskModifier = ResolveTravelModeRiskModifier(travelModePreset);
        var projectedRisk = Math.Clamp(
            (baseRisk * 0.70d) + (piratePressure * 35d) + (modeRiskModifier * 10d) - (economicDensity * 12d),
            0d,
            100d);
        var interdictionChance = Math.Clamp(
            (projectedRisk / 100d) * (0.55d + (piratePressure * 0.35d)),
            0d,
            0.98d);
        var protectionCost = decimal.Round(
            (decimal)(projectedRisk * hopCount * 12.5d + (plan.TotalFuelCost * 3.2d)),
            2);

        return new RouteRiskSimulation(
            BaseRiskScore: baseRisk,
            ProjectedRiskScore: projectedRisk,
            PiratePressure: piratePressure,
            EconomicDensity: economicDensity,
            InterdictionChance: interdictionChance,
            ProtectionCostEstimateCredits: protectionCost,
            ExpectedTravelTimeSeconds: Math.Max(plan.TotalTravelTimeSeconds, 0),
            RiskBand: ResolveRiskBand(projectedRisk));
    }

    internal static RouteOptimizationProfile SelectRecommendedProfile(RouteOptimizationApiDto optimization)
    {
        var candidates = new List<(RouteOptimizationProfile Profile, RoutePlanApiDto Plan)>();
        if (optimization.Fastest is not null)
        {
            candidates.Add((RouteOptimizationProfile.Fastest, optimization.Fastest));
        }

        if (optimization.Cheapest is not null)
        {
            candidates.Add((RouteOptimizationProfile.Cheapest, optimization.Cheapest));
        }

        if (optimization.Safest is not null)
        {
            candidates.Add((RouteOptimizationProfile.Safest, optimization.Safest));
        }

        if (optimization.Balanced is not null)
        {
            candidates.Add((RouteOptimizationProfile.Balanced, optimization.Balanced));
        }

        if (candidates.Count == 0)
        {
            return RouteOptimizationProfile.None;
        }

        return candidates
            .OrderBy(static candidate => ComputeOptimizationScore(candidate.Plan))
            .ThenBy(static candidate => candidate.Profile)
            .Select(static candidate => candidate.Profile)
            .First();
    }

    internal static string ResolveTravelMode(RouteTravelModePreset preset)
    {
        return preset switch
        {
            RouteTravelModePreset.HighBurn => "HighBurn",
            RouteTravelModePreset.StealthTransit => "StealthTransit",
            RouteTravelModePreset.Convoy => "Convoy",
            RouteTravelModePreset.GhostRoute => "GhostRoute",
            RouteTravelModePreset.ArmedEscort => "ArmedEscort",
            _ => "Standard"
        };
    }

    private static IReadOnlyList<StarmapRouteEdge> ToOverlayEdges(
        IEnumerable<RouteApiDto> routes,
        int highRiskThreshold)
    {
        return routes
            .Select(route => new StarmapRouteEdge(
                RouteId: route.Id,
                FromSectorId: route.FromSectorId,
                ToSectorId: route.ToSectorId,
                IsHighRisk: route.BaseRiskScore >= highRiskThreshold))
            .ToArray();
    }

    private static IReadOnlyList<StarmapRouteEdge> ToOverlayEdges(
        IEnumerable<RouteHopApiDto> hops,
        int highRiskThreshold)
    {
        return hops
            .Select(hop => new StarmapRouteEdge(
                RouteId: hop.RouteId,
                FromSectorId: hop.FromSectorId,
                ToSectorId: hop.ToSectorId,
                IsHighRisk: hop.BaseRiskScore >= highRiskThreshold))
            .ToArray();
    }

    private static IReadOnlyList<string> SplitWaypointTokens(string value)
    {
        return WaypointDelimiter
            .Split(value)
            .Select(static token => token.Trim())
            .Where(static token => !string.IsNullOrWhiteSpace(token))
            .ToArray();
    }

    private static RouteSectorOption ResolveSectorOrThrow(
        string token,
        IReadOnlyList<RouteSectorOption> sectors,
        string role)
    {
        var resolved = TryResolveSectorToken(token, sectors);
        if (resolved is not null)
        {
            return resolved;
        }

        throw new InvalidOperationException($"Could not resolve {role} sector token '{token}'.");
    }

    private static RouteSectorOption? TryResolveSectorToken(
        string token,
        IReadOnlyList<RouteSectorOption> sectors)
    {
        if (string.IsNullOrWhiteSpace(token) || sectors.Count == 0)
        {
            return null;
        }

        if (Guid.TryParse(token, out var sectorId))
        {
            return sectors.FirstOrDefault(sector => sector.SectorId == sectorId);
        }

        var normalizedToken = token.Trim();
        var exact = sectors.FirstOrDefault(
            sector => string.Equals(sector.Name, normalizedToken, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        var prefixMatches = sectors
            .Where(sector => sector.Name.StartsWith(normalizedToken, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (prefixMatches.Length == 1)
        {
            return prefixMatches[0];
        }

        var containsMatches = sectors
            .Where(sector => sector.Name.Contains(normalizedToken, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        return containsMatches.Length == 1
            ? containsMatches[0]
            : null;
    }

    private static IReadOnlyList<Guid> BuildSegmentChain(
        Guid fromSectorId,
        Guid toSectorId,
        IReadOnlyList<RouteSectorOption> waypoints)
    {
        var chain = new List<Guid>(waypoints.Count + 2)
        {
            fromSectorId
        };

        foreach (var waypoint in waypoints)
        {
            if (waypoint.SectorId == toSectorId || waypoint.SectorId == chain[^1])
            {
                continue;
            }

            chain.Add(waypoint.SectorId);
        }

        if (chain[^1] != toSectorId)
        {
            chain.Add(toSectorId);
        }

        return chain;
    }

    private static string ResolveSectorName(Guid sectorId, IReadOnlyList<RouteSectorOption> sectors)
    {
        return sectors.FirstOrDefault(sector => sector.SectorId == sectorId)?.Name
            ?? sectorId.ToString();
    }

    private static string NormalizeAlgorithm(string algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            return "dijkstra";
        }

        var normalized = algorithm.Trim().ToLowerInvariant();
        return normalized switch
        {
            "a*" => "astar",
            "a-star" => "astar",
            "astar" => "astar",
            _ => "dijkstra"
        };
    }

    private static RoutePlanApiDto MergeSegments(
        IReadOnlyList<RoutePlanApiDto> segments,
        string algorithm)
    {
        if (segments.Count == 0)
        {
            throw new InvalidOperationException("Route segments were empty.");
        }

        if (segments.Count == 1)
        {
            var single = segments[0];
            return new RoutePlanApiDto
            {
                FromSectorId = single.FromSectorId,
                ToSectorId = single.ToSectorId,
                Algorithm = algorithm,
                TravelMode = single.TravelMode,
                TotalCost = single.TotalCost,
                TotalTravelTimeSeconds = single.TotalTravelTimeSeconds,
                TotalFuelCost = single.TotalFuelCost,
                TotalRiskScore = single.TotalRiskScore,
                SectorPath = single.SectorPath.ToArray(),
                Hops = single.Hops.ToArray()
            };
        }

        var sectorPath = new List<Guid>();
        foreach (var segment in segments)
        {
            if (segment.SectorPath.Count == 0)
            {
                if (sectorPath.Count == 0)
                {
                    sectorPath.Add(segment.FromSectorId);
                }

                if (sectorPath[^1] != segment.ToSectorId)
                {
                    sectorPath.Add(segment.ToSectorId);
                }

                continue;
            }

            foreach (var sectorId in segment.SectorPath)
            {
                if (sectorPath.Count == 0 || sectorPath[^1] != sectorId)
                {
                    sectorPath.Add(sectorId);
                }
            }
        }

        return new RoutePlanApiDto
        {
            FromSectorId = segments[0].FromSectorId,
            ToSectorId = segments[^1].ToSectorId,
            Algorithm = algorithm,
            TravelMode = segments[0].TravelMode,
            TotalCost = segments.Sum(static segment => segment.TotalCost),
            TotalTravelTimeSeconds = segments.Sum(static segment => segment.TotalTravelTimeSeconds),
            TotalFuelCost = segments.Sum(static segment => segment.TotalFuelCost),
            TotalRiskScore = segments.Sum(static segment => segment.TotalRiskScore),
            SectorPath = sectorPath,
            Hops = segments.SelectMany(static segment => segment.Hops).ToArray()
        };
    }

    private static double EstimateEconomicDensity(
        IReadOnlyList<Guid> sectorPath,
        IReadOnlyList<RouteApiDto> routes)
    {
        if (sectorPath.Count == 0 || routes.Count == 0)
        {
            return 0d;
        }

        var degrees = new Dictionary<Guid, int>();
        foreach (var route in routes)
        {
            if (!degrees.TryAdd(route.FromSectorId, 1))
            {
                degrees[route.FromSectorId]++;
            }

            if (!degrees.TryAdd(route.ToSectorId, 1))
            {
                degrees[route.ToSectorId]++;
            }
        }

        if (degrees.Count == 0)
        {
            return 0d;
        }

        var maxDegree = degrees.Values.Max();
        if (maxDegree <= 0)
        {
            return 0d;
        }

        var sampled = sectorPath
            .Distinct()
            .Select(sectorId => degrees.TryGetValue(sectorId, out var degree) ? degree : 0)
            .ToArray();

        if (sampled.Length == 0)
        {
            return 0d;
        }

        var average = sampled.Average();
        return Math.Clamp(average / maxDegree, 0d, 1d);
    }

    private static double ResolveTravelModeRiskModifier(RouteTravelModePreset preset)
    {
        return preset switch
        {
            RouteTravelModePreset.HighBurn => 1.1d,
            RouteTravelModePreset.StealthTransit => -0.5d,
            RouteTravelModePreset.Convoy => -0.2d,
            RouteTravelModePreset.GhostRoute => -0.9d,
            RouteTravelModePreset.ArmedEscort => -1.1d,
            _ => 0d
        };
    }

    private static RouteRiskBand ResolveRiskBand(double projectedRiskScore)
    {
        return projectedRiskScore switch
        {
            < 30d => RouteRiskBand.Low,
            < 55d => RouteRiskBand.Elevated,
            < 75d => RouteRiskBand.High,
            _ => RouteRiskBand.Severe
        };
    }

    private static RoutePlanApiDto? GetPlanForProfile(
        RouteOptimizationApiDto optimization,
        RouteOptimizationProfile profile)
    {
        return profile switch
        {
            RouteOptimizationProfile.Fastest => optimization.Fastest,
            RouteOptimizationProfile.Cheapest => optimization.Cheapest,
            RouteOptimizationProfile.Safest => optimization.Safest,
            RouteOptimizationProfile.Balanced => optimization.Balanced,
            _ => optimization.Balanced ?? optimization.Fastest ?? optimization.Cheapest ?? optimization.Safest
        };
    }

    private static double ComputeOptimizationScore(RoutePlanApiDto plan)
    {
        return (plan.TotalRiskScore * 1.8d) +
               (plan.TotalTravelTimeSeconds * 0.02d) +
               (plan.TotalFuelCost * 0.1d);
    }
}
