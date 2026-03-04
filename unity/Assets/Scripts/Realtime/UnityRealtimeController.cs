using GalacticTrader.ClientSdk.Realtime;
using GalacticTrader.Desktop.Realtime;
using GalacticTrader.Unity.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Realtime;

public sealed class UnityRealtimeController : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";
    [SerializeField] private UnityAuthController? authController;
    [SerializeField] private int strategicIntervalSeconds = 5;
    [SerializeField] private string communicationChannelType = "global";
    [SerializeField] private string communicationChannelKey = "desktop-feed";

    private RealtimeCoordinator? _coordinator;

    public event Action<DashboardRealtimeSnapshotApiDto>? StrategicSnapshotReceived;

    public event Action<CommunicationRealtimeMessageApiDto>? CommunicationMessageReceived;

    public event Action<Exception>? Faulted;

    public event Action<RealtimeDiagnosticsSnapshot>? DiagnosticsChanged;

    public RealtimeDiagnosticsSnapshot? Diagnostics => _coordinator?.Diagnostics;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var session = authController?.CurrentSession;
        if (session is null)
        {
            return;
        }

        EnsureCoordinator(session.AccessToken);
        if (_coordinator is null)
        {
            return;
        }

        await _coordinator.StartAsync(
            session.PlayerId,
            strategicIntervalSeconds,
            communicationChannelType,
            communicationChannelKey,
            cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_coordinator is null)
        {
            return;
        }

        await _coordinator.StopAsync(cancellationToken);
    }

    private async void OnDestroy()
    {
        if (_coordinator is not null)
        {
            await _coordinator.DisposeAsync();
            _coordinator = null;
        }
    }

    private void EnsureCoordinator(string accessToken)
    {
        if (_coordinator is not null)
        {
            return;
        }

        var strategicClient = new StrategicRealtimeClientAdapter(
            new StrategicRealtimeStreamClient(apiBaseUrl, accessToken));
        var communicationClient = new CommunicationRealtimeClientAdapter(
            new CommunicationRealtimeStreamClient(apiBaseUrl, accessToken));
        _coordinator = new RealtimeCoordinator(strategicClient, communicationClient);
        _coordinator.StrategicSnapshotReceived += HandleStrategicSnapshotReceived;
        _coordinator.CommunicationMessageReceived += HandleCommunicationMessageReceived;
        _coordinator.Faulted += HandleFaulted;
        _coordinator.DiagnosticsChanged += HandleDiagnosticsChanged;
    }

    private void HandleStrategicSnapshotReceived(DashboardRealtimeSnapshotApiDto snapshot)
    {
        StrategicSnapshotReceived?.Invoke(snapshot);
    }

    private void HandleCommunicationMessageReceived(CommunicationRealtimeMessageApiDto message)
    {
        CommunicationMessageReceived?.Invoke(message);
    }

    private void HandleFaulted(Exception exception)
    {
        Faulted?.Invoke(exception);
    }

    private void HandleDiagnosticsChanged(RealtimeDiagnosticsSnapshot diagnostics)
    {
        DiagnosticsChanged?.Invoke(diagnostics);
    }
}
