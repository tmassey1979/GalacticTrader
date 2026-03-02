using GalacticTrader.Desktop.Splash;

namespace GalacticTrader.Desktop.Tests;

public sealed class SplashBootScriptTests
{
    [Fact]
    public void Lines_ContainExpectedBootEntries()
    {
        Assert.NotEmpty(SplashBootScript.Lines);
        Assert.Contains(SplashBootScript.Lines, line => line.Contains("[vault]", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("[ready] command interface online", SplashBootScript.Lines);
    }
}
