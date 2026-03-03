using GalacticTrader.Desktop.Routes;

namespace GalacticTrader.Desktop.Tests;

public sealed class RouteModePresetMapperTests
{
    [Theory]
    [InlineData("Safe Route", "Convoy")]
    [InlineData("Balanced Route", "Standard")]
    [InlineData("High Profit Route", "HighBurn")]
    [InlineData("Smuggler Route", "GhostRoute")]
    [InlineData("Standard", "Standard")]
    [InlineData("ArmedEscort", "ArmedEscort")]
    [InlineData("Unknown Value", "Standard")]
    public void ToApiMode_MapsPresetLabelsAndFallbacks(string input, string expected)
    {
        var mapped = RouteModePresetMapper.ToApiMode(input);

        Assert.Equal(expected, mapped);
    }
}
