using GalacticTrader.Desktop;

namespace GalacticTrader.Desktop.Tests;

public sealed class ModuleQuickActionsBuilderTests
{
    [Theory]
    [InlineData("Trading", "Preview Spread")]
    [InlineData("Routes", "Optimize Profiles")]
    [InlineData("Reputation", "Review Matrix")]
    public void Build_ReturnsModuleSpecificQuickActionHints(string module, string expectedFragment)
    {
        var text = ModuleQuickActionsBuilder.Build(module);

        Assert.Contains(expectedFragment, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ReturnsDefaultMessage_ForUnknownModule()
    {
        var text = ModuleQuickActionsBuilder.Build("Unknown");

        Assert.Equal("Quick Actions: Use module controls to inspect strategic data", text);
    }
}
