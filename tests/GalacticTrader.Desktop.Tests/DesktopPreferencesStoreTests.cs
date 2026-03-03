using GalacticTrader.Desktop.Settings;
using System.IO;

namespace GalacticTrader.Desktop.Tests;

public sealed class DesktopPreferencesStoreTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileIsMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gt-prefs-{Guid.NewGuid():N}.json");
        var store = new DesktopPreferencesStore(path);

        var preferences = store.Load();

        Assert.True(preferences.AutoRefreshEnabled);
        Assert.Equal(30, preferences.RefreshIntervalSeconds);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsPreferences()
    {
        var path = Path.Combine(Path.GetTempPath(), $"gt-prefs-{Guid.NewGuid():N}.json");
        try
        {
            var store = new DesktopPreferencesStore(path);
            var expected = new DesktopPreferences
            {
                AutoRefreshEnabled = false,
                ShowRiskBadges = false,
                RefreshIntervalSeconds = 45,
                RefreshDashboardHotkey = "Ctrl+Alt+D",
                RefreshEventsHotkey = "Ctrl+Alt+E"
            };

            store.Save(expected);
            var loaded = store.Load();

            Assert.False(loaded.AutoRefreshEnabled);
            Assert.False(loaded.ShowRiskBadges);
            Assert.Equal(45, loaded.RefreshIntervalSeconds);
            Assert.Equal("Ctrl+Alt+D", loaded.RefreshDashboardHotkey);
            Assert.Equal("Ctrl+Alt+E", loaded.RefreshEventsHotkey);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
