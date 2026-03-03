using System.Windows.Input;

namespace GalacticTrader.Desktop.Navigation;

public static class ModuleHotkeyNavigator
{
    public static bool TryResolveTabIndex(
        Key key,
        ModifierKeys modifiers,
        int tabCount,
        out int tabIndex)
    {
        tabIndex = -1;
        if ((modifiers & ModifierKeys.Control) == 0 || (modifiers & ModifierKeys.Alt) != 0)
        {
            return false;
        }

        tabIndex = key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            Key.D8 or Key.NumPad8 => 7,
            Key.D9 or Key.NumPad9 => 8,
            Key.D0 or Key.NumPad0 => 9,
            _ => -1
        };

        if (tabIndex < 0 || tabIndex >= tabCount)
        {
            tabIndex = -1;
            return false;
        }

        return true;
    }
}
