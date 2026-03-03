using System.IO;
using System.Text.Json;

namespace GalacticTrader.Desktop.Settings;

public sealed class DesktopPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public DesktopPreferencesStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GalacticTrader",
                "desktop-preferences.json")
            : filePath;
    }

    public string FilePath => _filePath;

    public DesktopPreferences Load()
    {
        if (!File.Exists(_filePath))
        {
            return new DesktopPreferences();
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new DesktopPreferences();
        }

        return JsonSerializer.Deserialize<DesktopPreferences>(json, SerializerOptions)
            ?? new DesktopPreferences();
    }

    public void Save(DesktopPreferences preferences)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(preferences, SerializerOptions);
        File.WriteAllText(_filePath, json);
    }
}
