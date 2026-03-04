using GalacticTrader.ClientSdk.Dashboard;
using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;
using GalacticTrader.Unity.Auth;
using GalacticTrader.Unity.Realtime;
using GalacticTrader.Unity.Shell;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Modules.Dashboard;

public sealed class UnityDashboardModuleController : UnityShellModule
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private UnityRealtimeController? realtimeController;
    [SerializeField] private int dangerousRouteRiskThreshold = 65;
    [SerializeField] private int maxRealtimeEvents = 200;

    private HttpClient? _httpClient;
    private DashboardModuleService? _dashboardService;

    public DashboardActionBoard? LastBoard { get; private set; }

    public IReadOnlyList<DashboardEventFeedEntry> LastEventFeed { get; private set; } = [];

    public event Action<DashboardActionBoard>? BoardUpdated;

    public event Action<IReadOnlyList<DashboardEventFeedEntry>>? EventFeedUpdated;

    public event Action<string>? LoadFailed;

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
            var state = await _dashboardService.LoadStateAsync(session.PlayerId, cancellationToken);
            LastBoard = state.Board;
            LastEventFeed = state.EventFeed;
            BoardUpdated?.Invoke(LastBoard);
            EventFeedUpdated?.Invoke(LastEventFeed);
        }
        catch (Exception exception)
        {
            LoadFailed?.Invoke(exception.Message);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeRealtime();
        _httpClient?.Dispose();
        _httpClient = null;
    }

    public IReadOnlyList<DashboardEventFeedEntry> GetFilteredEventFeed(
        DashboardEventFeedFilterOptions options,
        DateTime nowUtc)
    {
        return DashboardEventFeedFilter.Apply(LastEventFeed, options, nowUtc);
    }

    public string ExportEventFeedCsv(IReadOnlyList<DashboardEventFeedEntry> entries)
    {
        return DashboardEventFeedCsvExporter.BuildCsv(entries);
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
        if (LastBoard is null)
        {
            return;
        }

        var state = DashboardRealtimeStateProjector.ApplySnapshot(
            LastBoard,
            LastEventFeed,
            snapshot,
            maxRealtimeEvents);

        LastBoard = state.Board;
        LastEventFeed = state.EventFeed;
        BoardUpdated?.Invoke(LastBoard);
        EventFeedUpdated?.Invoke(LastEventFeed);
    }
}
