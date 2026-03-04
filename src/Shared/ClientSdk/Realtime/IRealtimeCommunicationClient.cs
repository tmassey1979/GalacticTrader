using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.ClientSdk.Realtime;

public interface IRealtimeCommunicationClient
{
    event Action<CommunicationRealtimeMessageApiDto>? MessageReceived;

    event Action<Exception>? ConnectionFaulted;

    void Start(Guid playerId, string channelType = "global", string channelKey = "desktop-feed");

    Task StopAsync();
}
