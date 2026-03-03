using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class VoiceActivityInputParserTests
{
    [Fact]
    public void TryParseRms_ClampsBetweenZeroAndOne()
    {
        Assert.True(VoiceActivityInputParser.TryParseRms("1.4", out var high));
        Assert.Equal(1f, high);
        Assert.True(VoiceActivityInputParser.TryParseRms("-0.5", out var low));
        Assert.Equal(0f, low);
    }

    [Fact]
    public void TryParsePercent_ClampsBetweenZeroAndHundred()
    {
        Assert.True(VoiceActivityInputParser.TryParsePercent("101.5", out var high));
        Assert.Equal(100f, high);
        Assert.True(VoiceActivityInputParser.TryParsePercent("-9", out var low));
        Assert.Equal(0f, low);
    }

    [Fact]
    public void TryParseMs_RejectsInvalidValues()
    {
        Assert.False(VoiceActivityInputParser.TryParseMs("n/a", out _));
        Assert.True(VoiceActivityInputParser.TryParseMs("-7.5", out var value));
        Assert.Equal(0f, value);
    }
}
