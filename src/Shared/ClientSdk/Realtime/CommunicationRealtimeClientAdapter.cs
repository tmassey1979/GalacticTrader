using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Realtime;

public sealed class CommunicationRealtimeClientAdapter : IRealtimeCommunicationClient
{
    private readonly CommunicationRealtimeStreamClient _innerClient;

    public CommunicationRealtimeClientAdapter(CommunicationRealtimeStreamClient innerClient)
    {
        _innerClient = innerClient;
    }

    public event Action<CommunicationRealtimeMessageApiDto>? MessageReceived
    {
        add => _innerClient.MessageReceived += value;
        remove => _innerClient.MessageReceived -= value;
    }

    public event Action<Exception>? ConnectionFaulted
    {
        add => _innerClient.ConnectionFaulted += value;
        remove => _innerClient.ConnectionFaulted -= value;
    }

    public void Start(Guid playerId, string channelType = "global", string channelKey = "desktop-feed")
    {
        _innerClient.Start(playerId, channelType, channelKey);
    }

    public Task StopAsync()
    {
        return _innerClient.StopAsync();
    }
}
