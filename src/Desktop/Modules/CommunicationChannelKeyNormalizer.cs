namespace GalacticTrader.Desktop.Modules;

public static class CommunicationChannelKeyNormalizer
{
    public static string Normalize(string channelKey)
    {
        return string.IsNullOrWhiteSpace(channelKey)
            ? "global"
            : channelKey.Trim().ToLowerInvariant();
    }
}
