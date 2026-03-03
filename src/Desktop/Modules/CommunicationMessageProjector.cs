using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class CommunicationMessageProjector
{
    public static IReadOnlyList<CommunicationMessageDisplayRow> Build(IReadOnlyList<CommunicationChannelMessageApiDto> messages)
    {
        return messages
            .OrderBy(static message => message.CreatedAt)
            .ThenBy(static message => message.Id)
            .Select(static message => new CommunicationMessageDisplayRow
            {
                CreatedAtUtc = DateTime.SpecifyKind(message.CreatedAt, DateTimeKind.Utc),
                Sender = ResolveSender(message.SenderId),
                Channel = $"{message.ChannelType}:{message.ChannelKey}",
                Content = message.Content,
                Moderation = message.IsModerated ? "filtered" : "raw"
            })
            .ToArray();
    }

    private static string ResolveSender(Guid senderId)
    {
        if (senderId == Guid.Empty)
        {
            return "system";
        }

        var compact = senderId.ToString("N");
        return compact[..8];
    }
}
