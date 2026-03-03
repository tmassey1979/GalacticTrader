using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class TerritoryEconomicPolicyInputParserTests
{
    [Theory]
    [InlineData("10.5", 0, 50, true, 10.5)]
    [InlineData("-7.25", -50, 50, true, -7.25)]
    [InlineData("55", 0, 50, false, 0)]
    [InlineData("abc", 0, 50, false, 0)]
    public void TryParsePercent_ValidatesBoundsAndFormat(
        string raw,
        decimal minimum,
        decimal maximum,
        bool expectedValid,
        decimal expectedValue)
    {
        var valid = TerritoryEconomicPolicyInputParser.TryParsePercent(raw, minimum, maximum, out var parsed);

        Assert.Equal(expectedValid, valid);
        Assert.Equal(expectedValue, parsed);
    }
}
