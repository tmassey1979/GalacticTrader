using System.Net;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Fleet;

public sealed class FleetModuleService
{
    private readonly FleetDataSource _dataSource;

    public FleetModuleService(FleetDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<FleetModuleState> LoadStateAsync(
        Guid playerId,
        string formation = "Defensive",
        CancellationToken cancellationToken = default)
    {
        var templatesTask = _dataSource.LoadShipTemplatesAsync(cancellationToken);
        var shipsTask = _dataSource.LoadShipsAsync(playerId, cancellationToken);
        var escortTask = _dataSource.LoadEscortSummaryAsync(playerId, formation, cancellationToken);

        await Task.WhenAll(templatesTask, shipsTask, escortTask);

        var templates = await templatesTask;
        var ships = await shipsTask;
        var escort = await escortTask;
        var summary = BuildSummary(ships, escort);
        return new FleetModuleState(
            templates,
            ships,
            escort,
            summary,
            DateTime.UtcNow);
    }

    public FleetModuleState ApplyRealtimeSnapshot(
        FleetModuleState currentState,
        DashboardRealtimeSnapshotApiDto snapshot)
    {
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(snapshot);
        var summary = currentState.Summary with
        {
            FleetStrength = snapshot.Metrics.FleetStrength,
            ProtectionStatus = string.IsNullOrWhiteSpace(snapshot.Metrics.ProtectionStatus)
                ? currentState.Summary.ProtectionStatus
                : snapshot.Metrics.ProtectionStatus
        };

        return currentState with
        {
            Summary = summary,
            LoadedAtUtc = snapshot.CapturedAtUtc
        };
    }

    public async Task<FleetOperationResult> PurchaseShipAsync(
        PurchaseShipApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PlayerId == Guid.Empty)
        {
            return FleetOperationResult.Failure(
                FleetOperationFailureState.Validation,
                "Player id is required to purchase a ship.");
        }

        if (string.IsNullOrWhiteSpace(request.TemplateKey))
        {
            return FleetOperationResult.Failure(
                FleetOperationFailureState.Validation,
                "Ship template is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return FleetOperationResult.Failure(
                FleetOperationFailureState.Validation,
                "Ship name is required.");
        }

        try
        {
            var ship = await _dataSource.PurchaseShipAsync(request, cancellationToken);
            return FleetOperationResult.Success(
                ship,
                $"Purchased {ship.Name} ({ship.ShipClass}).");
        }
        catch (Exception exception)
        {
            var failureState = ResolveFailureState(exception);
            return FleetOperationResult.Failure(
                failureState,
                ResolveFailureMessage(failureState),
                ResolveErrorDetail(exception));
        }
    }

    public Task<ConvoySimulationResultApiDto?> SimulateConvoyAsync(
        ConvoySimulationApiRequest request,
        CancellationToken cancellationToken = default)
    {
        return _dataSource.SimulateConvoyAsync(request, cancellationToken);
    }

    internal static FleetStatusSummary BuildSummary(
        IReadOnlyList<ShipApiDto> ships,
        EscortSummaryApiDto? escort)
    {
        var crewCapacity = ships.Sum(static ship => Math.Max(ship.CrewSlots, 0));
        var shipCount = ships.Count;
        var totalHullPercent = ships.Count == 0
            ? 0d
            : ships.Average(static ship => ship.MaxHullIntegrity <= 0
                ? 0d
                : Math.Clamp(ship.HullIntegrity / (double)ship.MaxHullIntegrity * 100d, 0d, 100d));
        var fleetStrength = escort?.FleetStrength ?? ships.Sum(static ship => ship.Hardpoints + ship.ShieldCapacity / 25);
        var protectionStatus = ResolveProtectionStatus(fleetStrength);
        return new FleetStatusSummary(
            ShipCount: shipCount,
            CrewCount: ships.Sum(static ship => Math.Max(ship.CrewCount, 0)),
            CrewCapacity: crewCapacity,
            InsuredShipCount: ships.Count(static ship => ship.HasInsurance),
            FleetValue: ships.Sum(static ship => ship.CurrentValue),
            AverageHullIntegrityPercent: Math.Round(totalHullPercent, 1),
            FleetStrength: fleetStrength,
            ProtectionStatus: protectionStatus,
            EscortCoordinationBonus: escort?.CoordinationBonus ?? 0f);
    }

    private static FleetOperationFailureState ResolveFailureState(Exception exception)
    {
        if (exception is ApiClientException apiException)
        {
            if (apiException.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return FleetOperationFailureState.Unauthorized;
            }

            if (apiException.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return FleetOperationFailureState.RateLimited;
            }
        }

        var detail = ResolveErrorDetail(exception);
        if (detail.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            detail.Contains("must", StringComparison.OrdinalIgnoreCase))
        {
            return FleetOperationFailureState.Validation;
        }

        return FleetOperationFailureState.Unknown;
    }

    private static string ResolveFailureMessage(FleetOperationFailureState failureState)
    {
        return failureState switch
        {
            FleetOperationFailureState.Validation => "Fleet action is invalid. Verify template and request details.",
            FleetOperationFailureState.Unauthorized => "Session is not authorized for this fleet action.",
            FleetOperationFailureState.RateLimited => "Fleet actions are temporarily rate-limited. Retry shortly.",
            _ => "Fleet action failed. Refresh fleet data and try again."
        };
    }

    private static string ResolveErrorDetail(Exception exception)
    {
        return exception is ApiClientException apiException
            ? apiException.Detail
            : exception.Message;
    }

    private static string ResolveProtectionStatus(int fleetStrength)
    {
        return fleetStrength switch
        {
            >= 150 => "Fortified",
            >= 80 => "Guarded",
            >= 30 => "Contested",
            > 0 => "Fragile",
            _ => "Unprotected"
        };
    }
}
