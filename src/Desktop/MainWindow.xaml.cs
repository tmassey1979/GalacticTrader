using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Battles;
using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Fleet;
using GalacticTrader.Desktop.Intel;
using GalacticTrader.Desktop.Modules;
using GalacticTrader.Desktop.Routes;
using GalacticTrader.Desktop.Settings;
using GalacticTrader.Desktop.Starmap;
using GalacticTrader.Desktop.Trading;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GalacticTrader.Desktop;

public partial class MainWindow : Window
{
    private readonly OrbitCameraController _cameraController = new();
    private readonly StarmapScene _scene;
    private readonly DesktopSession _session;
    private readonly NavigationApiClient _navigationApiClient;
    private readonly MarketApiClient _marketApiClient;
    private readonly FleetApiClient _fleetApiClient;
    private readonly ReputationApiClient _reputationApiClient;
    private readonly StrategicApiClient _strategicApiClient;
    private readonly NpcApiClient _npcApiClient;
    private readonly CombatApiClient _combatApiClient;
    private readonly DesktopHotkeyBindings _hotkeyBindings;
    private readonly ObservableCollection<EventFeedEntry> _filteredEventFeed = [];
    private List<EventFeedEntry> _eventFeedAll = [];
    private bool _isOrbiting;
    private Point _lastMousePoint;
    private bool _isUpdatingSliders;
    private bool _hasLoaded;

    public MainWindow(
        StarmapScene scene,
        DesktopSession session,
        NavigationApiClient navigationApiClient,
        EconomyApiClient economyApiClient,
        MarketApiClient marketApiClient,
        FleetApiClient fleetApiClient,
        ReputationApiClient reputationApiClient,
        StrategicApiClient strategicApiClient,
        NpcApiClient npcApiClient,
        CombatApiClient combatApiClient)
    {
        _scene = scene;
        _session = session;
        _navigationApiClient = navigationApiClient;
        _marketApiClient = marketApiClient;
        _fleetApiClient = fleetApiClient;
        _reputationApiClient = reputationApiClient;
        _strategicApiClient = strategicApiClient;
        _npcApiClient = npcApiClient;
        _combatApiClient = combatApiClient;
        _hotkeyBindings = DesktopHotkeyBindings.FromPreferences(new DesktopPreferencesStore().Load());

        InitializeComponent();
        BuildStarmap(_scene);
        DashboardHost.Content = new DashboardPanel(
            _session,
            _scene,
            _navigationApiClient,
            _marketApiClient,
            _fleetApiClient,
            _reputationApiClient,
            _strategicApiClient);
        TradingHost.Content = new TradingPanel(_session, economyApiClient, _marketApiClient);
        RoutesHost.Content = new RoutePlanningPanel(_navigationApiClient);
        BattlesHost.Content = new BattlePanel(_combatApiClient);
        FleetHost.Content = new FleetPanel(_session, fleetApiClient);
        IntelHost.Content = new IntelPanel(_session, navigationApiClient, reputationApiClient, strategicApiClient);
        ServicesHost.Content = new ServicesPanel(_npcApiClient);
        ReputationHost.Content = new ReputationPanel(_session, _reputationApiClient);
        TerritoryHost.Content = new TerritoryPanel(_strategicApiClient);
        AnalyticsHost.Content = new AnalyticsPanel(_session, _marketApiClient, _combatApiClient);
        SettingsHost.Content = new SettingsPanel(_session);
        EventFeedGrid.ItemsSource = _filteredEventFeed;
        PlayerMetricText.Text = $"Player: {_session.Username}";
        ApplyCamera(updateSliders: true);

        if (!string.IsNullOrWhiteSpace(session.Username))
        {
            Title = $"Galactic Trader Command - {session.Username}";
        }

        StarViewport.MouseLeftButtonDown += OnViewportMouseLeftButtonDown;
        StarViewport.MouseLeftButtonUp += OnViewportMouseLeftButtonUp;
        StarViewport.MouseMove += OnViewportMouseMove;
        StarViewport.MouseWheel += OnViewportMouseWheel;
        PreviewKeyDown += OnMainWindowPreviewKeyDown;
        Loaded += OnMainWindowLoaded;
    }

    private void BuildStarmap(StarmapScene scene)
    {
        RouteList.ItemsSource = scene.Routes;
        SceneModels.Content = scene.Models;
    }

    private void ApplyCamera(bool updateSliders)
    {
        var pose = _cameraController.BuildPose();
        SceneCamera.Position = pose.Position;
        SceneCamera.LookDirection = pose.LookDirection;
        SceneCamera.UpDirection = pose.UpDirection;

        if (!updateSliders)
        {
            return;
        }

        _isUpdatingSliders = true;
        YawSlider.Value = _cameraController.YawDegrees;
        PitchSlider.Value = _cameraController.PitchDegrees;
        ZoomSlider.Value = _cameraController.Distance;
        _isUpdatingSliders = false;
    }

    private void OnViewportMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isOrbiting = true;
        _lastMousePoint = e.GetPosition(StarViewport);
        StarViewport.CaptureMouse();
    }

    private void OnViewportMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isOrbiting = false;
        StarViewport.ReleaseMouseCapture();
    }

    private void OnViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isOrbiting)
        {
            return;
        }

        var current = e.GetPosition(StarViewport);
        var delta = current - _lastMousePoint;
        _lastMousePoint = current;

        _cameraController.OrbitBy(delta);
        ApplyCamera(updateSliders: true);
    }

    private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _cameraController.ZoomBy(e.Delta);
        ApplyCamera(updateSliders: true);
    }

    private void OnCameraSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSliders)
        {
            return;
        }

        _cameraController.SetFromSliders(
            yawDegrees: YawSlider.Value,
            pitchDegrees: PitchSlider.Value,
            distance: ZoomSlider.Value);
        ApplyCamera(updateSliders: false);
    }

    private void OnResetCameraClick(object sender, RoutedEventArgs e)
    {
        _cameraController.Reset();
        ApplyCamera(updateSliders: true);
    }

    private void OnRouteSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RouteList.SelectedItem is not RouteSegment selected)
        {
            return;
        }

        _cameraController.FocusOnRoute(selected);
        ApplyCamera(updateSliders: true);
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await RefreshDashboardAndEventsAsync();
    }

    private async void OnRefreshDashboardClick(object sender, RoutedEventArgs e)
    {
        await RefreshDashboardAndEventsAsync();
    }

    private async void OnRefreshEventFeedClick(object sender, RoutedEventArgs e)
    {
        await RefreshDashboardAndEventsAsync();
    }

    private void OnEventFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyEventFilter();
    }

    private void OnEventKeywordTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyEventFilter();
    }

    private void OnExportEventFeedClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var entries = _filteredEventFeed.ToArray();
            var csv = EventFeedCsvExporter.BuildCsv(entries);
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GalacticTrader");
            Directory.CreateDirectory(root);
            var filename = $"event-feed-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            var path = Path.Combine(root, filename);
            File.WriteAllText(path, csv);
            MessageBox.Show($"Event feed exported to {path}", "Galactic Trader", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Event feed export failed: {exception.Message}",
                "Galactic Trader",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnMainWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;
        if (_hotkeyBindings.DashboardRefresh.Matches(key, modifiers))
        {
            e.Handled = true;
            _ = RefreshDashboardAndEventsAsync();
            return;
        }

        if (_hotkeyBindings.EventRefresh.Matches(key, modifiers))
        {
            e.Handled = true;
            _ = RefreshDashboardAndEventsAsync();
        }
    }

    private async Task RefreshDashboardAndEventsAsync()
    {
        SetDashboardBusy(true);
        try
        {
            var transactionsTask = _marketApiClient.GetTransactionsAsync(_session.PlayerId, limit: 25);
            var standingsTask = _reputationApiClient.GetFactionStandingsAsync(_session.PlayerId);
            var escortTask = _fleetApiClient.GetEscortSummaryAsync(_session.PlayerId);
            var routesTask = _navigationApiClient.GetDangerousRoutesAsync(65);
            var reportsTask = _strategicApiClient.GetIntelligenceReportsAsync(_session.PlayerId);
            var combatLogsTask = _combatApiClient.GetRecentLogsAsync(limit: 25);
            await Task.WhenAll(transactionsTask, standingsTask, escortTask, routesTask, reportsTask, combatLogsTask);

            var transactions = transactionsTask.Result;
            var standings = standingsTask.Result;
            var escort = escortTask.Result;
            var routes = routesTask.Result;
            var reports = reportsTask.Result;
            var combatLogs = combatLogsTask.Result;

            var threats = ThreatAlertRanker.Build(routes, reports);
            var metrics = StatusMetricAggregator.Build(transactions, standings, escort, threats, _scene);
            CreditsMetricText.Text = $"Credits: {metrics.LiquidCredits:N2}";
            ReputationMetricText.Text = $"Reputation: {metrics.ReputationScore}";
            FleetMetricText.Text = $"Fleet: {metrics.FleetStrength}";
            RoutesMetricText.Text = $"Routes: {metrics.ActiveRoutes}";
            AlertsMetricText.Text = $"Alerts: {metrics.AlertCount}";

            _eventFeedAll = EventFeedBuilder.Build(transactions, combatLogs, reports, DateTime.UtcNow).ToList();
            ApplyEventFilter();
        }
        catch (Exception exception)
        {
            AlertsMetricText.Text = $"Alerts: error";
            MessageBox.Show(
                $"Dashboard refresh failed: {exception.Message}",
                "Galactic Trader",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            SetDashboardBusy(false);
        }
    }

    private void ApplyEventFilter()
    {
        var selectedCategory = (EventCategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        var keyword = EventKeywordFilter.Text ?? string.Empty;
        var window = (EventTimeWindowFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var options = new EventFeedFilterOptions
        {
            Category = selectedCategory,
            Keyword = keyword,
            MaxAge = window switch
            {
                "Last 1h" => TimeSpan.FromHours(1),
                "Last 24h" => TimeSpan.FromHours(24),
                "Last 7d" => TimeSpan.FromDays(7),
                _ => null
            }
        };

        var filtered = EventFeedFilter.Apply(_eventFeedAll, options, DateTime.UtcNow);

        _filteredEventFeed.Clear();
        foreach (var entry in filtered)
        {
            _filteredEventFeed.Add(entry);
        }
    }

    private void SetDashboardBusy(bool isBusy)
    {
        RefreshDashboardButton.IsEnabled = !isBusy;
        RefreshEventFeedButton.IsEnabled = !isBusy;
    }
}
