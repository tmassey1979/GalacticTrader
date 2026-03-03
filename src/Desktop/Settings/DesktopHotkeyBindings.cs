using System.Windows.Input;

namespace GalacticTrader.Desktop.Settings;

public sealed class DesktopHotkeyBindings
{
    public required DesktopHotkeyBinding DashboardRefresh { get; init; }
    public required DesktopHotkeyBinding EventRefresh { get; init; }

    public static DesktopHotkeyBindings FromPreferences(DesktopPreferences preferences)
    {
        var dashboardDefault = new DesktopHotkeyBinding
        {
            Key = Key.D,
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift
        };

        var eventsDefault = new DesktopHotkeyBinding
        {
            Key = Key.E,
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift
        };

        return new DesktopHotkeyBindings
        {
            DashboardRefresh = DesktopHotkeyBinding.ParseOrDefault(preferences.RefreshDashboardHotkey, dashboardDefault),
            EventRefresh = DesktopHotkeyBinding.ParseOrDefault(preferences.RefreshEventsHotkey, eventsDefault)
        };
    }
}
