using GalacticTrader.Desktop.Navigation;

namespace GalacticTrader.Desktop.Tests;

public sealed class ModuleBreadcrumbBuilderTests
{
    [Theory]
    [InlineData("Dashboard", "Command / Dashboard")]
    [InlineData(" Market Intel ", "Command / Market Intel")]
    [InlineData("", "Command / Dashboard")]
    [InlineData(null, "Command / Dashboard")]
    public void Build_ReturnsNormalizedBreadcrumb(string? input, string expected)
    {
        var breadcrumb = ModuleBreadcrumbBuilder.Build(input);
        Assert.Equal(expected, breadcrumb);
    }
}
