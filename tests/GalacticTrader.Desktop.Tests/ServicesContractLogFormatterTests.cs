using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class ServicesContractLogFormatterTests
{
    [Fact]
    public void Build_FormatsNormalizedLogLine()
    {
        var at = new DateTime(2026, 3, 3, 9, 30, 15, DateTimeKind.Utc);

        var line = ServicesContractLogFormatter.Build(at, "offer-contract", "Aegis", "goal defend");

        Assert.Equal("09:30:15Z | offer-contract | Aegis | goal defend", line);
    }

    [Fact]
    public void Build_UsesFallbackTokens_WhenValuesMissing()
    {
        var at = new DateTime(2026, 3, 3, 9, 30, 15, DateTimeKind.Utc);

        var line = ServicesContractLogFormatter.Build(at, " ", " ", " ");

        Assert.Equal("09:30:15Z | action | unknown-agent | completed", line);
    }
}
