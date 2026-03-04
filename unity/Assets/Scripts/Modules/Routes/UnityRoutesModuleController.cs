using GalacticTrader.ClientSdk.Routes;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Unity.Auth;
using GalacticTrader.Unity.Shell;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Modules.Routes;

public sealed class UnityRoutesModuleController : UnityShellModule
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private int dangerousRouteRiskThreshold = 70;

    private HttpClient? _httpClient;
    private RouteModuleService? _routeService;

    public RouteModuleState? LastState { get; private set; }

    public RoutePlanningResult? LastPlan { get; private set; }

    public RouteOptimizationView? LastOptimization { get; private set; }

    public event Action<RouteModuleState>? StateUpdated;

    public event Action<RoutePlanningResult>? PlanUpdated;

    public event Action<RouteOptimizationView>? OptimizationUpdated;

    public event Action<RouteOverlayState>? OverlayUpdated;

    public event Action<string>? OperationFailed;

    public override async Task OnActivatedAsync(CancellationToken cancellationToken)
    {
        await base.OnActivatedAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            OperationFailed?.Invoke("No active session. Sign in before loading routes.");
            return;
        }

        EnsureService(session.AccessToken);
        if (_routeService is null)
        {
            OperationFailed?.Invoke("Routes service is not initialized.");
            return;
        }

        try
        {
            LastState = await _routeService.LoadStateAsync(dangerousRouteRiskThreshold, cancellationToken);
            StateUpdated?.Invoke(LastState);
            OverlayUpdated?.Invoke(LastState.Overlay);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task PlanRouteAsync(
        string fromSectorToken,
        string toSectorToken,
        string? waypointInput,
        RouteTravelModePreset preset = RouteTravelModePreset.Standard,
        string algorithm = "dijkstra",
        CancellationToken cancellationToken = default)
    {
        if (_routeService is null)
        {
            OperationFailed?.Invoke("Routes service is not initialized.");
            return;
        }

        if (LastState is null)
        {
            await RefreshAsync(cancellationToken);
        }

        if (LastState is null)
        {
            OperationFailed?.Invoke("Routes state is not available.");
            return;
        }

        try
        {
            LastPlan = await _routeService.PlanRouteAsync(
                LastState,
                fromSectorToken,
                toSectorToken,
                waypointInput,
                preset,
                algorithm,
                dangerousRouteRiskThreshold,
                cancellationToken);
            PlanUpdated?.Invoke(LastPlan);
            OverlayUpdated?.Invoke(LastPlan.Overlay);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task LoadOptimizationsAsync(
        string fromSectorToken,
        string toSectorToken,
        CancellationToken cancellationToken = default)
    {
        if (_routeService is null)
        {
            OperationFailed?.Invoke("Routes service is not initialized.");
            return;
        }

        if (LastState is null)
        {
            await RefreshAsync(cancellationToken);
        }

        if (LastState is null)
        {
            OperationFailed?.Invoke("Routes state is not available.");
            return;
        }

        try
        {
            LastOptimization = await _routeService.LoadOptimizationAsync(
                LastState,
                fromSectorToken,
                toSectorToken,
                dangerousRouteRiskThreshold,
                cancellationToken);
            OptimizationUpdated?.Invoke(LastOptimization);
            OverlayUpdated?.Invoke(LastOptimization.Overlay);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    private void OnDestroy()
    {
        _httpClient?.Dispose();
        _httpClient = null;
    }

    private void EnsureService(string accessToken)
    {
        if (_routeService is not null && _httpClient is not null)
        {
            return;
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
        };

        var navigationApiClient = new NavigationApiClient(_httpClient);
        navigationApiClient.SetBearerToken(accessToken);

        var dataSource = new RouteDataSource
        {
            LoadSectorsAsync = cancellationToken => navigationApiClient.GetSectorsAsync(cancellationToken),
            LoadRoutesAsync = cancellationToken => navigationApiClient.GetRoutesAsync(cancellationToken),
            LoadDangerousRoutesAsync = (riskThreshold, cancellationToken) => navigationApiClient.GetDangerousRoutesAsync(riskThreshold, cancellationToken),
            LoadRoutePlanAsync = (fromSectorId, toSectorId, travelMode, algorithm, cancellationToken) => navigationApiClient.GetRoutePlanAsync(fromSectorId, toSectorId, travelMode, algorithm, cancellationToken),
            LoadRouteOptimizationAsync = (fromSectorId, toSectorId, cancellationToken) => navigationApiClient.GetRouteOptimizationAsync(fromSectorId, toSectorId, cancellationToken)
        };

        _routeService = new RouteModuleService(dataSource);
    }
}
