using GalacticTrader.ClientSdk.Fleet;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;
using GalacticTrader.Unity.Auth;
using GalacticTrader.Unity.Realtime;
using GalacticTrader.Unity.Shell;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Modules.Fleet;

public sealed class UnityFleetModuleController : UnityShellModule
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private UnityRealtimeController? realtimeController;
    [SerializeField] private string escortFormation = "Defensive";

    private HttpClient? _httpClient;
    private FleetModuleService? _fleetService;

    public FleetModuleState? LastState { get; private set; }

    public ShipApiDto? LastPurchasedShip { get; private set; }

    public ConvoySimulationResultApiDto? LastConvoySimulation { get; private set; }

    public event Action<FleetModuleState>? StateUpdated;

    public event Action<ShipApiDto>? ShipPurchased;

    public event Action<ConvoySimulationResultApiDto>? ConvoySimulationUpdated;

    public event Action<string>? OperationFailed;

    public override async Task OnActivatedAsync(CancellationToken cancellationToken)
    {
        await base.OnActivatedAsync(cancellationToken);
        SubscribeRealtime();
        await RefreshAsync(cancellationToken);
    }

    public override Task OnDeactivatedAsync(CancellationToken cancellationToken)
    {
        UnsubscribeRealtime();
        return base.OnDeactivatedAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before loading fleet.");
            return;
        }

        EnsureService(session.AccessToken);
        if (_fleetService is null)
        {
            OperationFailed?.Invoke("Fleet service is not initialized.");
            return;
        }

        try
        {
            LastState = await _fleetService.LoadStateAsync(
                session.PlayerId,
                escortFormation,
                cancellationToken);
            StateUpdated?.Invoke(LastState);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task PurchaseShipAsync(
        string templateKey,
        string shipName,
        CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before purchasing ships.");
            return;
        }

        if (_fleetService is null)
        {
            OperationFailed?.Invoke("Fleet service is not initialized.");
            return;
        }

        var result = await _fleetService.PurchaseShipAsync(
            new PurchaseShipApiRequest
            {
                PlayerId = session.PlayerId,
                TemplateKey = templateKey,
                Name = shipName
            },
            cancellationToken);

        if (!result.Succeeded || result.Ship is null)
        {
            OperationFailed?.Invoke(result.Message);
            return;
        }

        LastPurchasedShip = result.Ship;
        ShipPurchased?.Invoke(result.Ship);
        await RefreshAsync(cancellationToken);
    }

    public async Task SimulateConvoyAsync(
        decimal convoyValue,
        int formation = 1,
        CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before convoy simulation.");
            return;
        }

        if (_fleetService is null)
        {
            OperationFailed?.Invoke("Fleet service is not initialized.");
            return;
        }

        try
        {
            LastConvoySimulation = await _fleetService.SimulateConvoyAsync(
                new ConvoySimulationApiRequest
                {
                    PlayerId = session.PlayerId,
                    Formation = formation,
                    ConvoyValue = convoyValue
                },
                cancellationToken);

            if (LastConvoySimulation is null)
            {
                OperationFailed?.Invoke("Convoy simulation returned no result.");
                return;
            }

            ConvoySimulationUpdated?.Invoke(LastConvoySimulation);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeRealtime();
        _httpClient?.Dispose();
        _httpClient = null;
    }

    private void EnsureService(string accessToken)
    {
        if (_fleetService is not null && _httpClient is not null)
        {
            return;
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
        };

        var fleetApiClient = new FleetApiClient(_httpClient);
        fleetApiClient.SetBearerToken(accessToken);

        var dataSource = new FleetDataSource
        {
            LoadShipTemplatesAsync = cancellationToken => fleetApiClient.GetShipTemplatesAsync(cancellationToken),
            LoadShipsAsync = (playerId, cancellationToken) => fleetApiClient.GetPlayerShipsAsync(playerId, cancellationToken),
            LoadEscortSummaryAsync = (playerId, formation, cancellationToken) => fleetApiClient.GetEscortSummaryAsync(playerId, formation, cancellationToken),
            PurchaseShipAsync = (request, cancellationToken) => fleetApiClient.PurchaseShipAsync(request, cancellationToken),
            SimulateConvoyAsync = (request, cancellationToken) => fleetApiClient.SimulateConvoyAsync(request, cancellationToken)
        };

        _fleetService = new FleetModuleService(dataSource);
    }

    private void SubscribeRealtime()
    {
        if (realtimeController is null)
        {
            return;
        }

        realtimeController.StrategicSnapshotReceived -= OnStrategicSnapshotReceived;
        realtimeController.StrategicSnapshotReceived += OnStrategicSnapshotReceived;
    }

    private void UnsubscribeRealtime()
    {
        if (realtimeController is null)
        {
            return;
        }

        realtimeController.StrategicSnapshotReceived -= OnStrategicSnapshotReceived;
    }

    private void OnStrategicSnapshotReceived(DashboardRealtimeSnapshotApiDto snapshot)
    {
        if (_fleetService is null || LastState is null)
        {
            return;
        }

        LastState = _fleetService.ApplyRealtimeSnapshot(LastState, snapshot);
        StateUpdated?.Invoke(LastState);
    }
}
