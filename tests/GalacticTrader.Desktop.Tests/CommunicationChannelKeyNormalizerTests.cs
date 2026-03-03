using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class CommunicationChannelKeyNormalizerTests
{
    [Fact]
    public void Normalize_UsesGlobal_WhenBlank()
    {
        var normalized = CommunicationChannelKeyNormalizer.Normalize("   ");
        Assert.Equal("global", normalized);
    }

    [Fact]
    public void Normalize_TrimsAndLowercases()
    {
        var normalized = CommunicationChannelKeyNormalizer.Normalize(" Fleet-ALPHA ");
        Assert.Equal("fleet-alpha", normalized);
    }
}
