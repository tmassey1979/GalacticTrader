using GalacticTrader.Desktop.Realtime;

namespace GalacticTrader.Desktop.Tests;

public sealed class RealtimeReconnectPolicyTests
{
    [Fact]
    public void GetDelay_GrowsExponentiallyUntilMax()
    {
        var policy = new RealtimeReconnectPolicy
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(5),
            Multiplier = 2d
        };

        Assert.Equal(TimeSpan.FromSeconds(1), policy.GetDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(4), policy.GetDelay(3));
        Assert.Equal(TimeSpan.FromSeconds(5), policy.GetDelay(4));
        Assert.Equal(TimeSpan.FromSeconds(5), policy.GetDelay(8));
    }
}
