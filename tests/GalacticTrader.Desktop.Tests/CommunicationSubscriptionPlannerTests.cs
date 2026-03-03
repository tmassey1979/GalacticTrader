using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class CommunicationSubscriptionPlannerTests
{
    [Fact]
    public void Plan_RequestsInitialSubscribe_WhenNoCurrentSubscription()
    {
        var transition = CommunicationSubscriptionPlanner.Plan(
            currentChannelType: null,
            currentChannelKey: null,
            nextChannelType: 0,
            nextChannelKey: "global");

        Assert.False(transition.ShouldUnsubscribeCurrent);
        Assert.True(transition.ShouldSubscribeNext);
    }

    [Fact]
    public void Plan_NoAction_WhenCurrentMatchesNext()
    {
        var transition = CommunicationSubscriptionPlanner.Plan(
            currentChannelType: 0,
            currentChannelKey: "desktop-feed",
            nextChannelType: 0,
            nextChannelKey: "Desktop-Feed");

        Assert.False(transition.ShouldUnsubscribeCurrent);
        Assert.False(transition.ShouldSubscribeNext);
    }

    [Fact]
    public void Plan_UnsubscribeAndSubscribe_WhenChannelChanges()
    {
        var transition = CommunicationSubscriptionPlanner.Plan(
            currentChannelType: 0,
            currentChannelKey: "desk",
            nextChannelType: 2,
            nextChannelKey: "faction-ops");

        Assert.True(transition.ShouldUnsubscribeCurrent);
        Assert.True(transition.ShouldSubscribeNext);
    }
}
