using System.Windows.Input;

namespace GalacticTrader.Desktop.Settings;

public sealed class DesktopHotkeyBinding
{
    public Key Key { get; init; }
    public ModifierKeys Modifiers { get; init; }

    public bool Matches(Key key, ModifierKeys modifiers)
    {
        return Key == key && Modifiers == modifiers;
    }

    public static DesktopHotkeyBinding ParseOrDefault(string? raw, DesktopHotkeyBinding fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var tokens = raw
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (tokens.Length == 0)
        {
            return fallback;
        }

        var modifiers = ModifierKeys.None;
        for (var index = 0; index < tokens.Length - 1; index++)
        {
            modifiers |= tokens[index].ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKeys.Control,
                "shift" => ModifierKeys.Shift,
                "alt" => ModifierKeys.Alt,
                "win" or "windows" => ModifierKeys.Windows,
                _ => ModifierKeys.None
            };
        }

        var keyToken = tokens[^1];
        if (!Enum.TryParse<Key>(keyToken, ignoreCase: true, out var key))
        {
            return fallback;
        }

        return new DesktopHotkeyBinding
        {
            Key = key,
            Modifiers = modifiers
        };
    }
}
