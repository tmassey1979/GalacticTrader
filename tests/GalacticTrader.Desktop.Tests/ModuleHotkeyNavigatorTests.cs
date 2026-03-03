using GalacticTrader.Desktop.Navigation;
using System.Windows.Input;

namespace GalacticTrader.Desktop.Tests;

public sealed class ModuleHotkeyNavigatorTests
{
    [Theory]
    [InlineData(Key.D1, 0)]
    [InlineData(Key.D5, 4)]
    [InlineData(Key.D0, 9)]
    [InlineData(Key.NumPad3, 2)]
    public void TryResolveTabIndex_MapsCtrlDigits(Key key, int expected)
    {
        var ok = ModuleHotkeyNavigator.TryResolveTabIndex(
            key,
            ModifierKeys.Control,
            tabCount: 12,
            out var actual);

        Assert.True(ok);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryResolveTabIndex_ReturnsFalse_WhenModifierInvalidOrOutOfBounds()
    {
        var noControl = ModuleHotkeyNavigator.TryResolveTabIndex(Key.D1, ModifierKeys.None, 12, out _);
        var withAlt = ModuleHotkeyNavigator.TryResolveTabIndex(Key.D2, ModifierKeys.Control | ModifierKeys.Alt, 12, out _);
        var outOfBounds = ModuleHotkeyNavigator.TryResolveTabIndex(Key.D9, ModifierKeys.Control, 2, out _);

        Assert.False(noControl);
        Assert.False(withAlt);
        Assert.False(outOfBounds);
    }
}
