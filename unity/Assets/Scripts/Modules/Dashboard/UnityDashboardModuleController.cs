using GalacticTrader.ClientSdk.Dashboard;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Unity.Auth;
using GalacticTrader.Unity.Shell;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Modules.Dashboard;

public sealed class UnityDashboardModuleController : UnityShellModule
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private int dangerousRouteRiskThreshold = 65;

    private HttpClient? _httpClient;
    private DashboardModuleService? _dashboardService;

    public DashboardActionBoard? LastBoard { get; private set; }

    public event Action<DashboardActionBoard>? BoardUpdated;

    public event Action<string>? LoadFailed;

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
            LoadFailed?.Invoke("No active session. Sign in before loading dashboard.");
            return;
        }

        EnsureService(session.AccessToken);
        if (_dashboardService is null)
        {
            LoadFailed?.Invoke("Dashboard service is not initialized.");
            return;
        }

        try
        {
            LastBoard = await _dashboardService.LoadBoardAsync(session.PlayerId, cancellationToken);
            BoardUpdated?.Invoke(LastBoard);
        }
        catch (Exception exception)
        {
            LoadFailed?.Invoke(exception.Message);
        }
    }

    private void OnDestroy()
    {
        _httpClient?.Dispose();
        _httpClient = null;
    }

    private void EnsureService(string accessToken)
    {
        if (_dashboardService is not null && _httpClient is not null)
        {
            return;
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
        };

        var navigationApiClient = new NavigationApiClient(_httpClient);
        navigationApiClient.SetBearerToken(accessToken);
        var marketApiClient = new MarketApiClient(_httpClient);
        marketApiClient.SetBearerToken(accessToken);
        var fleetApiClient = new FleetApiClient(_httpClient);
        fleetApiClient.SetBearerToken(accessToken);
        var reputationApiClient = new ReputationApiClient(_httpClient);
        reputationApiClient.SetBearerToken(accessToken);
        var strategicApiClient = new StrategicApiClient(_httpClient);
        strategicApiClient.SetBearerToken(accessToken);
        var telemetryApiClient = new TelemetryApiClient(_httpClient);
        telemetryApiClient.SetBearerToken(accessToken);

        var dataSource = new DashboardDataSource
        {
            LoadTransactionsAsync = (playerId, cancellationToken) => marketApiClient.GetTransactionsAsync(playerId, limit: 25, cancellationToken),
            LoadShipsAsync = (playerId, cancellationToken) => fleetApiClient.GetPlayerShipsAsync(playerId, cancellationToken),
            LoadEscortAsync = (playerId, cancellationToken) => fleetApiClient.GetEscortSummaryAsync(playerId, cancellationToken: cancellationToken),
            LoadStandingsAsync = (playerId, cancellationToken) => reputationApiClient.GetFactionStandingsAsync(playerId, cancellationToken),
            LoadDangerousRoutesAsync = (_, cancellationToken) => navigationApiClient.GetDangerousRoutesAsync(dangerousRouteRiskThreshold, cancellationToken),
            LoadIntelligenceAsync = (playerId, cancellationToken) => strategicApiClient.GetIntelligenceReportsAsync(playerId, cancellationToken),
            LoadGlobalMetricsAsync = cancellationToken => telemetryApiClient.GetGlobalSummaryAsync(cancellationToken)
        };

        _dashboardService = new DashboardModuleService(dataSource);
    }
}
