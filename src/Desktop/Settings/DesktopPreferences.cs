namespace GalacticTrader.Desktop.Settings;

public sealed class DesktopPreferences
{
    public bool AutoRefreshEnabled { get; init; } = true;
    public bool ShowRiskBadges { get; init; } = true;
    public int RefreshIntervalSeconds { get; init; } = 30;
    public string RefreshDashboardHotkey { get; init; } = "Ctrl+Shift+D";
    public string RefreshEventsHotkey { get; init; } = "Ctrl+Shift+E";
}
