using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Realtime;

public sealed class RealtimeCoordinator : IAsyncDisposable
{
    private readonly IRealtimeStrategicClient _strategicClient;
    private readonly IRealtimeCommunicationClient _communicationClient;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private bool _disposed;
    private bool _running;
    private bool _handlersAttached;
    private Guid? _activePlayerId;

    private long _strategicSnapshotCount;
    private long _communicationMessageCount;
    private long _strategicFaultCount;
    private long _communicationFaultCount;
    private DateTimeOffset? _lastStrategicMessageAtUtc;
    private DateTimeOffset? _lastCommunicationMessageAtUtc;
    private DateTimeOffset? _lastFaultAtUtc;

    public RealtimeCoordinator(
        IRealtimeStrategicClient strategicClient,
        IRealtimeCommunicationClient communicationClient)
    {
        _strategicClient = strategicClient;
        _communicationClient = communicationClient;
    }

    public event Action<DashboardRealtimeSnapshotApiDto>? StrategicSnapshotReceived;

    public event Action<CommunicationRealtimeMessageApiDto>? CommunicationMessageReceived;

    public event Action<Exception>? Faulted;

    public event Action<RealtimeDiagnosticsSnapshot>? DiagnosticsChanged;

    public RealtimeDiagnosticsSnapshot Diagnostics => BuildSnapshot();

    public async Task StartAsync(
        Guid playerId,
        int strategicIntervalSeconds = 5,
        string channelType = "global",
        string channelKey = "desktop-feed",
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_running && _activePlayerId == playerId)
            {
                PublishDiagnostics();
                return;
            }

            if (_running)
            {
                await StopCoreAsync();
            }

            AttachHandlersIfNeeded();

            _strategicClient.Start(playerId, strategicIntervalSeconds);
            _communicationClient.Start(playerId, channelType, channelKey);

            _activePlayerId = playerId;
            _running = true;
            PublishDiagnostics();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _lifecycleGate.WaitAsync();
        try
        {
            await StopCoreAsync();
            DetachHandlersIfNeeded();
        }
        finally
        {
            _lifecycleGate.Release();
            _lifecycleGate.Dispose();
        }
    }

    private async Task StopCoreAsync()
    {
        if (_running)
        {
            await _strategicClient.StopAsync();
            await _communicationClient.StopAsync();
        }

        _running = false;
        _activePlayerId = null;
        PublishDiagnostics();
    }

    private void AttachHandlersIfNeeded()
    {
        if (_handlersAttached)
        {
            return;
        }

        _strategicClient.SnapshotReceived += OnStrategicSnapshotReceived;
        _strategicClient.ConnectionFaulted += OnStrategicFaulted;
        _communicationClient.MessageReceived += OnCommunicationMessageReceived;
        _communicationClient.ConnectionFaulted += OnCommunicationFaulted;
        _handlersAttached = true;
    }

    private void DetachHandlersIfNeeded()
    {
        if (!_handlersAttached)
        {
            return;
        }

        _strategicClient.SnapshotReceived -= OnStrategicSnapshotReceived;
        _strategicClient.ConnectionFaulted -= OnStrategicFaulted;
        _communicationClient.MessageReceived -= OnCommunicationMessageReceived;
        _communicationClient.ConnectionFaulted -= OnCommunicationFaulted;
        _handlersAttached = false;
    }

    private void OnStrategicSnapshotReceived(DashboardRealtimeSnapshotApiDto snapshot)
    {
        if (!_running)
        {
            return;
        }

        Interlocked.Increment(ref _strategicSnapshotCount);
        _lastStrategicMessageAtUtc = DateTimeOffset.UtcNow;
        StrategicSnapshotReceived?.Invoke(snapshot);
        PublishDiagnostics();
    }

    private void OnCommunicationMessageReceived(CommunicationRealtimeMessageApiDto message)
    {
        if (!_running)
        {
            return;
        }

        Interlocked.Increment(ref _communicationMessageCount);
        _lastCommunicationMessageAtUtc = DateTimeOffset.UtcNow;
        CommunicationMessageReceived?.Invoke(message);
        PublishDiagnostics();
    }

    private void OnStrategicFaulted(Exception exception)
    {
        if (!_running)
        {
            return;
        }

        Interlocked.Increment(ref _strategicFaultCount);
        _lastFaultAtUtc = DateTimeOffset.UtcNow;
        Faulted?.Invoke(exception);
        PublishDiagnostics();
    }

    private void OnCommunicationFaulted(Exception exception)
    {
        if (!_running)
        {
            return;
        }

        Interlocked.Increment(ref _communicationFaultCount);
        _lastFaultAtUtc = DateTimeOffset.UtcNow;
        Faulted?.Invoke(exception);
        PublishDiagnostics();
    }

    private void PublishDiagnostics()
    {
        DiagnosticsChanged?.Invoke(BuildSnapshot());
    }

    private RealtimeDiagnosticsSnapshot BuildSnapshot()
    {
        return new RealtimeDiagnosticsSnapshot(
            IsRunning: _running,
            PlayerId: _activePlayerId,
            StrategicSnapshotCount: Volatile.Read(ref _strategicSnapshotCount),
            CommunicationMessageCount: Volatile.Read(ref _communicationMessageCount),
            StrategicFaultCount: Volatile.Read(ref _strategicFaultCount),
            CommunicationFaultCount: Volatile.Read(ref _communicationFaultCount),
            LastStrategicMessageAtUtc: _lastStrategicMessageAtUtc,
            LastCommunicationMessageAtUtc: _lastCommunicationMessageAtUtc,
            LastFaultAtUtc: _lastFaultAtUtc);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
