using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Settings;
using System.Windows.Controls;

namespace GalacticTrader.Desktop.Modules;

public partial class SettingsPanel : UserControl
{
    private readonly DesktopPreferencesStore _preferencesStore;

    public SettingsPanel(DesktopSession session)
    {
        _preferencesStore = new DesktopPreferencesStore();
        InitializeComponent();
        HeaderText.Text = $"Settings - {session.Username}";
        PathText.Text = _preferencesStore.FilePath;
        LoadPreferences();
    }

    private void OnSaveClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!int.TryParse(RefreshIntervalText.Text.Trim(), out var refreshInterval) || refreshInterval < 5 || refreshInterval > 3600)
        {
            StatusText.Text = "Refresh interval must be between 5 and 3600 seconds.";
            return;
        }

        var preferences = new DesktopPreferences
        {
            AutoRefreshEnabled = AutoRefreshCheck.IsChecked == true,
            ShowRiskBadges = ShowRiskBadgesCheck.IsChecked == true,
            RefreshIntervalSeconds = refreshInterval,
            RefreshDashboardHotkey = NormalizeHotkey(DashboardHotkeyText.Text, "Ctrl+Shift+D"),
            RefreshEventsHotkey = NormalizeHotkey(EventsHotkeyText.Text, "Ctrl+Shift+E")
        };

        _preferencesStore.Save(preferences);
        StatusText.Text = "Preferences saved.";
    }

    private void LoadPreferences()
    {
        var preferences = _preferencesStore.Load();
        AutoRefreshCheck.IsChecked = preferences.AutoRefreshEnabled;
        ShowRiskBadgesCheck.IsChecked = preferences.ShowRiskBadges;
        RefreshIntervalText.Text = preferences.RefreshIntervalSeconds.ToString();
        DashboardHotkeyText.Text = preferences.RefreshDashboardHotkey;
        EventsHotkeyText.Text = preferences.RefreshEventsHotkey;
        StatusText.Text = "Preferences loaded.";
    }

    private static string NormalizeHotkey(string value, string fallback)
    {
        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
