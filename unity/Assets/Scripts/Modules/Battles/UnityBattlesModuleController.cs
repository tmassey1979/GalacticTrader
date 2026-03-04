using GalacticTrader.ClientSdk.Battles;
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

namespace GalacticTrader.Unity.Modules.Battles;

public sealed class UnityBattlesModuleController : UnityShellModule
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private UnityRealtimeController? realtimeController;
    [SerializeField] private int logLimit = 60;
    [SerializeField] private int maxRealtimeLogs = 120;

    private HttpClient? _httpClient;
    private BattlesModuleService? _battlesService;

    public BattlesModuleState? LastState { get; private set; }

    public CombatSummaryApiDto? LastCombatSummary { get; private set; }

    public CombatTickResultApiDto? LastTickResult { get; private set; }

    public event Action<BattlesModuleState>? StateUpdated;

    public event Action<CombatSummaryApiDto>? CombatUpdated;

    public event Action<CombatTickResultApiDto>? CombatTicked;

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
            OperationFailed?.Invoke("No active session. Sign in before loading battles.");
            return;
        }

        EnsureService(session.AccessToken);
        if (_battlesService is null)
        {
            OperationFailed?.Invoke("Battles service is not initialized.");
            return;
        }

        try
        {
            LastState = await _battlesService.LoadStateAsync(logLimit, cancellationToken);
            StateUpdated?.Invoke(LastState);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task StartCombatAsync(
        Guid attackerShipId,
        Guid defenderShipId,
        int maxTicks = 600,
        CancellationToken cancellationToken = default)
    {
        if (_battlesService is null)
        {
            OperationFailed?.Invoke("Battles service is not initialized.");
            return;
        }

        try
        {
            LastCombatSummary = await _battlesService.StartCombatAsync(
                new StartCombatApiRequest
                {
                    AttackerShipId = attackerShipId,
                    DefenderShipId = defenderShipId,
                    MaxTicks = Math.Max(1, maxTicks)
                },
                cancellationToken);

            CombatUpdated?.Invoke(LastCombatSummary);
            await RefreshAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task TickCombatAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        if (_battlesService is null)
        {
            OperationFailed?.Invoke("Battles service is not initialized.");
            return;
        }

        try
        {
            LastTickResult = await _battlesService.TickCombatAsync(combatId, cancellationToken);
            if (LastTickResult is null)
            {
                OperationFailed?.Invoke("Combat session was not found for tick processing.");
                return;
            }

            CombatTicked?.Invoke(LastTickResult);
            LastCombatSummary = await _battlesService.LoadCombatAsync(combatId, cancellationToken);
            if (LastCombatSummary is not null)
            {
                CombatUpdated?.Invoke(LastCombatSummary);
            }
        }
        catch (Exception exception)
        {
            OperationFailed?.Invoke(exception.Message);
        }
    }

    public async Task EndCombatAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        if (_battlesService is null)
        {
            OperationFailed?.Invoke("Battles service is not initialized.");
            return;
        }

        try
        {
            LastCombatSummary = await _battlesService.EndCombatAsync(combatId, cancellationToken);
            if (LastCombatSummary is null)
            {
                OperationFailed?.Invoke("Combat session was not found to end.");
                return;
            }

            CombatUpdated?.Invoke(LastCombatSummary);
            await RefreshAsync(cancellationToken);
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
        if (_battlesService is not null && _httpClient is not null)
        {
            return;
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl.TrimEnd('/'))
        };

        var combatApiClient = new CombatApiClient(_httpClient);
        combatApiClient.SetBearerToken(accessToken);

        var dataSource = new BattlesDataSource
        {
            LoadRecentLogsAsync = (limit, cancellationToken) => combatApiClient.GetRecentLogsAsync(limit, cancellationToken),
            LoadActiveCombatsAsync = cancellationToken => combatApiClient.GetActiveCombatsAsync(cancellationToken),
            StartCombatAsync = (request, cancellationToken) => combatApiClient.StartCombatAsync(request, cancellationToken),
            LoadCombatAsync = (combatId, cancellationToken) => combatApiClient.GetCombatAsync(combatId, cancellationToken),
            TickCombatAsync = (combatId, cancellationToken) => combatApiClient.ProcessTickAsync(combatId, cancellationToken),
            EndCombatAsync = (combatId, cancellationToken) => combatApiClient.EndCombatAsync(combatId, cancellationToken)
        };

        _battlesService = new BattlesModuleService(dataSource);
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
        if (_battlesService is null || LastState is null)
        {
            return;
        }

        LastState = _battlesService.ApplyRealtimeSnapshot(LastState, snapshot, maxRealtimeLogs);
        StateUpdated?.Invoke(LastState);
    }
}
