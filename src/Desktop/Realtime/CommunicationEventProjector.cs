using GalacticTrader.Desktop.Dashboard;

namespace GalacticTrader.Desktop.Realtime;

public static class CommunicationEventProjector
{
    public static EventFeedEntry Project(CommunicationRealtimeMessageApiDto message)
    {
        var moderationSuffix = message.IsModerated ? " [moderated]" : string.Empty;
        return new EventFeedEntry
        {
            OccurredAtUtc = message.CreatedAt.ToUniversalTime(),
            Category = "Comm",
            Title = $"{message.ChannelType}:{message.ChannelKey}",
            Detail = $"{message.Content}{moderationSuffix}"
        };
    }
}
