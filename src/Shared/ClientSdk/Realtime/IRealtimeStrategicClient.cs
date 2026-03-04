using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Realtime;

public interface IRealtimeStrategicClient
{
    event Action<DashboardRealtimeSnapshotApiDto>? SnapshotReceived;

    event Action<Exception>? ConnectionFaulted;

    void Start(Guid playerId, int intervalSeconds = 5);

    Task StopAsync();
}
