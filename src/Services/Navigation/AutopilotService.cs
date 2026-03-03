namespace GalacticTrader.Services.Navigation;

using GalacticTrader.Data.Repositories.Navigation;
using Microsoft.Extensions.Logging;

public sealed class AutopilotService : IAutopilotService
{
    private static readonly HashSet<(TravelMode From, TravelMode To)> AllowedTransitions =
    [
        (TravelMode.Standard, TravelMode.HighBurn),
        (TravelMode.Standard, TravelMode.StealthTransit),
        (TravelMode.Standard, TravelMode.Convoy),
        (TravelMode.Standard, TravelMode.ArmedEscort),
        (TravelMode.HighBurn, TravelMode.Standard),
        (TravelMode.HighBurn, TravelMode.StealthTransit),
        (TravelMode.StealthTransit, TravelMode.Standard),
        (TravelMode.StealthTransit, TravelMode.GhostRoute),
        (TravelMode.Convoy, TravelMode.Standard),
        (TravelMode.ArmedEscort, TravelMode.Standard),
        (TravelMode.GhostRoute, TravelMode.StealthTransit),
        (TravelMode.GhostRoute, TravelMode.Standard)
    ];

    private static readonly Lock LockObj = new();
    private static readonly Dictionary<Guid, AutopilotSessionInternal> Sessions = [];

    private readonly IRoutePlanningService _routePlanningService;
    private readonly ISectorRepository _sectorRepository;
    private readonly ILogger<AutopilotService> _logger;

    public AutopilotService(
        IRoutePlanningService routePlanningService,
        ISectorRepository sectorRepository,
        ILogger<AutopilotService> logger)
    {
        _routePlanningService = routePlanningService;
        _sectorRepository = sectorRepository;
        _logger = logger;
    }

    public async Task<AutopilotSessionDto> StartAutopilotAsync(
        StartAutopilotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ShipId == Guid.Empty)
        {
            throw new InvalidOperationException("ShipId is required.");
        }

        var routePlan = await _routePlanningService.CalculateRouteAsync(
            request.FromSectorId,
            request.ToSectorId,
            request.TravelMode,
            "dijkstra",
            cancellationToken);

        if (routePlan is null)
        {
            throw new InvalidOperationException("No valid route exists for the selected sectors.");
        }

        var session = new AutopilotSessionInternal
        {
            SessionId = Guid.NewGuid(),
            ShipId = request.ShipId,
            StartedAtUtc = DateTime.UtcNow,
            State = AutopilotState.Traveling,
            TravelMode = request.TravelMode,
            RoutePlan = routePlan,
            CurrentHopIndex = 0,
            RemainingSecondsInHop = routePlan.Hops.Count == 0
                ? 0
                : ApplyTravelModeTime(routePlan.Hops[0].BaseTravelTimeSeconds, request.TravelMode),
            CargoValue = request.CargoValue,
            PlayerNotoriety = request.PlayerNotoriety,
            EscortStrength = request.EscortStrength,
            FactionProtection = request.FactionProtection
        };

        lock (LockObj)
        {
            Sessions[session.SessionId] = session;
        }

        _logger.LogInformation(
            "Autopilot session started. SessionId={SessionId}, ShipId={ShipId}, Route={From}->{To}, Mode={Mode}",
            session.SessionId,
            session.ShipId,
            request.FromSectorId,
            request.ToSectorId,
            request.TravelMode);

        return MapSession(session);
    }

    public Task<AutopilotSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        lock (LockObj)
        {
            return Task.FromResult(
                Sessions.TryGetValue(sessionId, out var session)
                    ? MapSession(session)
                    : null);
        }
    }

    public Task<IReadOnlyList<AutopilotSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        lock (LockObj)
        {
            var result = Sessions.Values
                .Where(session => session.State is AutopilotState.Traveling or AutopilotState.Encounter or AutopilotState.Paused)
                .Select(MapSession)
                .ToList();
            return Task.FromResult((IReadOnlyList<AutopilotSessionDto>)result);
        }
    }

    public async Task<AutopilotTickResultDto?> ProcessTickAsync(
        Guid sessionId,
        int tickSeconds = 1,
        CancellationToken cancellationToken = default)
    {
        AutopilotSessionInternal? session;
        lock (LockObj)
        {
            Sessions.TryGetValue(sessionId, out session);
        }

        if (session is null)
        {
            return null;
        }

        return await ProcessSessionTickUnsafeAsync(session, Math.Max(1, tickSeconds), cancellationToken);
    }

    public async Task<IReadOnlyList<AutopilotTickResultDto>> ProcessActiveTicksAsync(
        int tickSeconds = 1,
        CancellationToken cancellationToken = default)
    {
        List<AutopilotSessionInternal> sessions;
        lock (LockObj)
        {
            sessions = Sessions.Values
                .Where(session => session.State is AutopilotState.Traveling or AutopilotState.Encounter)
                .ToList();
        }

        var results = new List<AutopilotTickResultDto>(sessions.Count);
        foreach (var session in sessions)
        {
            var result = await ProcessSessionTickUnsafeAsync(session, Math.Max(1, tickSeconds), cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<AutopilotSessionDto?> TransitionTravelModeAsync(
        Guid sessionId,
        TravelMode targetMode,
        string reason,
        CancellationToken cancellationToken = default)
    {
        AutopilotSessionInternal? session;
        lock (LockObj)
        {
            Sessions.TryGetValue(sessionId, out session);
        }

        if (session is null)
        {
            return null;
        }

        if (session.State is AutopilotState.Completed or AutopilotState.Cancelled or AutopilotState.Failed)
        {
            throw new InvalidOperationException("Cannot transition travel mode for a completed session.");
        }

        if (session.TravelMode == targetMode)
        {
            return MapSession(session);
        }

        if (!AllowedTransitions.Contains((session.TravelMode, targetMode)))
        {
            throw new InvalidOperationException(
                $"Transition from '{session.TravelMode}' to '{targetMode}' is not allowed.");
        }

        var previousMode = session.TravelMode;
        session.TravelMode = targetMode;
        session.RoutePlan = await _routePlanningService.CalculateRouteAsync(
            session.RoutePlan.FromSectorId,
            session.RoutePlan.ToSectorId,
            targetMode,
            "dijkstra",
            cancellationToken) ?? session.RoutePlan;

        if (session.CurrentHopIndex < session.RoutePlan.Hops.Count)
        {
            var baseHopTime = session.RoutePlan.Hops[session.CurrentHopIndex].BaseTravelTimeSeconds;
            session.RemainingSecondsInHop = Math.Min(
                ApplyTravelModeTime(baseHopTime, targetMode),
                Math.Max(1, session.RemainingSecondsInHop));
        }

        _logger.LogInformation(
            "Autopilot mode transition. SessionId={SessionId}, FromMode={FromMode}, ToMode={ToMode}, Reason={Reason}",
            session.SessionId,
            previousMode,
            targetMode,
            reason);

        return MapSession(session);
    }

    public Task<bool> CancelAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        lock (LockObj)
        {
            if (!Sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(false);
            }

            session.State = AutopilotState.Cancelled;
            session.CompletedAtUtc = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }

    private async Task<AutopilotTickResultDto> ProcessSessionTickUnsafeAsync(
        AutopilotSessionInternal session,
        int tickSeconds,
        CancellationToken cancellationToken)
    {
        var newEvents = new List<EncounterEventDto>();
        if (session.State is not (AutopilotState.Traveling or AutopilotState.Encounter))
        {
            return MapTickResult(session, newEvents);
        }

        if (session.CurrentHopIndex >= session.RoutePlan.Hops.Count)
        {
            session.State = AutopilotState.Completed;
            session.CompletedAtUtc = DateTime.UtcNow;
            return MapTickResult(session, newEvents);
        }

        session.State = AutopilotState.Traveling;
        session.TotalElapsedSeconds += tickSeconds;
        session.RemainingSecondsInHop -= tickSeconds;

        var currentHop = session.RoutePlan.Hops[session.CurrentHopIndex];
        var sector = await _sectorRepository.GetByIdAsync(currentHop.ToSectorId, cancellationToken);
        var sectorHazard = sector?.HazardRating ?? 0;

        // Encounter score formula:
        // BaseRouteRisk + SectorHazard + CargoValueFactor + PlayerNotoriety
        // - EscortStrength - FactionProtection - StealthModifier
        var cargoValueFactor = (double)Math.Clamp(session.CargoValue / 50_000m, 0, 100);
        var stealthModifier = session.TravelMode is TravelMode.StealthTransit or TravelMode.GhostRoute ? 25 : 0;

        var encounterScore = currentHop.BaseRiskScore
            + sectorHazard
            + cargoValueFactor
            + session.PlayerNotoriety
            - session.EscortStrength
            - session.FactionProtection
            - stealthModifier;

        var probability = Math.Clamp(encounterScore / 200d, 0.0d, 0.95d);
        if (Random.Shared.NextDouble() <= probability * (tickSeconds / 10d))
        {
            session.State = AutopilotState.Encounter;
            var encounter = new EncounterEventDto
            {
                EventId = Guid.NewGuid(),
                SessionId = session.SessionId,
                OccurredAtUtc = DateTime.UtcNow,
                SectorId = currentHop.ToSectorId,
                EventType = ResolveEncounterType(session.TravelMode),
                EncounterScore = Math.Round(encounterScore, 2),
                Probability = Math.Round(probability, 3),
                Description = "Encounter generated during autopilot tick processing."
            };
            session.EncounterEvents.Add(encounter);
            newEvents.Add(encounter);
        }

        while (session.RemainingSecondsInHop <= 0)
        {
            var overflow = -session.RemainingSecondsInHop;
            session.CurrentHopIndex++;

            if (session.CurrentHopIndex >= session.RoutePlan.Hops.Count)
            {
                session.State = AutopilotState.Completed;
                session.CompletedAtUtc = DateTime.UtcNow;
                session.RemainingSecondsInHop = 0;
                break;
            }

            var nextHop = session.RoutePlan.Hops[session.CurrentHopIndex];
            session.RemainingSecondsInHop = ApplyTravelModeTime(nextHop.BaseTravelTimeSeconds, session.TravelMode) - overflow;
            session.State = AutopilotState.Traveling;
        }

        return MapTickResult(session, newEvents);
    }

    private static int ApplyTravelModeTime(int baseTimeSeconds, TravelMode mode)
    {
        var multiplier = mode switch
        {
            TravelMode.HighBurn => 0.65d,
            TravelMode.StealthTransit => 1.25d,
            TravelMode.Convoy => 1.35d,
            TravelMode.GhostRoute => 1.70d,
            TravelMode.ArmedEscort => 1.15d,
            _ => 1.0d
        };

        return Math.Max(1, (int)Math.Round(baseTimeSeconds * multiplier));
    }

    private static string ResolveEncounterType(TravelMode mode)
    {
        return mode switch
        {
            TravelMode.GhostRoute => "SensorSweep",
            TravelMode.HighBurn => "HeatSignatureInterception",
            TravelMode.StealthTransit => "PatrolInspection",
            TravelMode.ArmedEscort => "Skirmish",
            TravelMode.Convoy => "ConvoyRaid",
            _ => "PirateAmbush"
        };
    }

    private static AutopilotTickResultDto MapTickResult(
        AutopilotSessionInternal session,
        List<EncounterEventDto> newEvents)
    {
        return new AutopilotTickResultDto
        {
            SessionId = session.SessionId,
            State = session.State,
            CurrentHopIndex = session.CurrentHopIndex,
            TotalHops = session.RoutePlan.Hops.Count,
            RemainingSecondsInHop = session.RemainingSecondsInHop,
            TotalElapsedSeconds = session.TotalElapsedSeconds,
            TotalRemainingSeconds = CalculateRemainingSeconds(session),
            CurrentMode = session.TravelMode,
            NewEvents = newEvents
        };
    }

    private static AutopilotSessionDto MapSession(AutopilotSessionInternal session)
    {
        return new AutopilotSessionDto
        {
            SessionId = session.SessionId,
            ShipId = session.ShipId,
            FromSectorId = session.RoutePlan.FromSectorId,
            ToSectorId = session.RoutePlan.ToSectorId,
            StartedAtUtc = session.StartedAtUtc,
            CompletedAtUtc = session.CompletedAtUtc,
            State = session.State,
            TravelMode = session.TravelMode,
            RoutePlan = session.RoutePlan,
            CurrentHopIndex = session.CurrentHopIndex,
            TotalElapsedSeconds = session.TotalElapsedSeconds,
            RemainingSecondsInHop = session.RemainingSecondsInHop,
            TotalRemainingSeconds = CalculateRemainingSeconds(session),
            CargoValue = session.CargoValue,
            PlayerNotoriety = session.PlayerNotoriety,
            EscortStrength = session.EscortStrength,
            FactionProtection = session.FactionProtection,
            EncounterEvents = [.. session.EncounterEvents]
        };
    }

    private static int CalculateRemainingSeconds(AutopilotSessionInternal session)
    {
        if (session.CurrentHopIndex >= session.RoutePlan.Hops.Count)
        {
            return 0;
        }

        var remaining = Math.Max(0, session.RemainingSecondsInHop);
        for (var index = session.CurrentHopIndex + 1; index < session.RoutePlan.Hops.Count; index++)
        {
            remaining += ApplyTravelModeTime(session.RoutePlan.Hops[index].BaseTravelTimeSeconds, session.TravelMode);
        }

        return remaining;
    }
}
