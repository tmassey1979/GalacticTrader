using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Realtime;

public sealed class StrategicRealtimeClientAdapter : IRealtimeStrategicClient
{
    private readonly StrategicRealtimeStreamClient _innerClient;

    public StrategicRealtimeClientAdapter(StrategicRealtimeStreamClient innerClient)
    {
        _innerClient = innerClient;
    }

    public event Action<DashboardRealtimeSnapshotApiDto>? SnapshotReceived
    {
        add => _innerClient.SnapshotReceived += value;
        remove => _innerClient.SnapshotReceived -= value;
    }

    public event Action<Exception>? ConnectionFaulted
    {
        add => _innerClient.ConnectionFaulted += value;
        remove => _innerClient.ConnectionFaulted -= value;
    }

    public void Start(Guid playerId, int intervalSeconds = 5)
    {
        _innerClient.Start(playerId, intervalSeconds);
    }

    public Task StopAsync()
    {
        return _innerClient.StopAsync();
    }
}
