namespace GalacticTrader.Desktop.Modules;

public static class CommunicationSubscriptionPlanner
{
    public static CommunicationSubscriptionTransition Plan(
        int? currentChannelType,
        string? currentChannelKey,
        int nextChannelType,
        string nextChannelKey)
    {
        var normalizedNextKey = CommunicationChannelKeyNormalizer.Normalize(nextChannelKey);
        var normalizedCurrentKey = currentChannelKey is null
            ? null
            : CommunicationChannelKeyNormalizer.Normalize(currentChannelKey);

        if (!currentChannelType.HasValue || string.IsNullOrWhiteSpace(normalizedCurrentKey))
        {
            return new CommunicationSubscriptionTransition
            {
                ShouldUnsubscribeCurrent = false,
                ShouldSubscribeNext = true
            };
        }

        if (currentChannelType.Value == nextChannelType &&
            string.Equals(normalizedCurrentKey, normalizedNextKey, StringComparison.Ordinal))
        {
            return new CommunicationSubscriptionTransition
            {
                ShouldUnsubscribeCurrent = false,
                ShouldSubscribeNext = false
            };
        }

        return new CommunicationSubscriptionTransition
        {
            ShouldUnsubscribeCurrent = true,
            ShouldSubscribeNext = true
        };
    }
}
