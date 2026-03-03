namespace GalacticTrader.Desktop.Modules;

public sealed class CommunicationSubscriptionTransition
{
    public bool ShouldUnsubscribeCurrent { get; init; }
    public bool ShouldSubscribeNext { get; init; }
}
