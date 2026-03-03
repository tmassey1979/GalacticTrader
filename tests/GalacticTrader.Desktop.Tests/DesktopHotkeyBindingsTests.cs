using GalacticTrader.Desktop.Settings;
using System.Windows.Input;

namespace GalacticTrader.Desktop.Tests;

public sealed class DesktopHotkeyBindingsTests
{
    [Fact]
    public void ParseOrDefault_ParsesValidChord()
    {
        var fallback = new DesktopHotkeyBinding { Key = Key.F1, Modifiers = ModifierKeys.None };

        var binding = DesktopHotkeyBinding.ParseOrDefault("Ctrl+Shift+D", fallback);

        Assert.Equal(Key.D, binding.Key);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, binding.Modifiers);
    }

    [Fact]
    public void ParseOrDefault_UsesFallbackForInvalidChord()
    {
        var fallback = new DesktopHotkeyBinding { Key = Key.F2, Modifiers = ModifierKeys.Alt };

        var binding = DesktopHotkeyBinding.ParseOrDefault("NotAKey", fallback);

        Assert.Equal(Key.F2, binding.Key);
        Assert.Equal(ModifierKeys.Alt, binding.Modifiers);
    }

    [Fact]
    public void FromPreferences_BuildsMatchableBindings()
    {
        var preferences = new DesktopPreferences
        {
            RefreshDashboardHotkey = "Ctrl+Shift+R",
            RefreshEventsHotkey = "Alt+E"
        };

        var bindings = DesktopHotkeyBindings.FromPreferences(preferences);

        Assert.True(bindings.DashboardRefresh.Matches(Key.R, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.True(bindings.EventRefresh.Matches(Key.E, ModifierKeys.Alt));
    }
}
