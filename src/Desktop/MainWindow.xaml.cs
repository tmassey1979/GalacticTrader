using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Battles;
using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Fleet;
using GalacticTrader.Desktop.Intel;
using GalacticTrader.Desktop.Modules;
using GalacticTrader.Desktop.Navigation;
using GalacticTrader.Desktop.Realtime;
using GalacticTrader.Desktop.Routes;
using GalacticTrader.Desktop.Settings;
using GalacticTrader.Desktop.Starmap;
using GalacticTrader.Desktop.Trading;
using Serilog;
using System.Diagnostics;
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
    private readonly StrategicRealtimeStreamClient _strategicRealtimeClient;
    private readonly CommunicationRealtimeStreamClient _communicationRealtimeClient;
    private readonly DesktopHotkeyBindings _hotkeyBindings;
    private readonly ObservableCollection<EventFeedEntry> _filteredEventFeed = [];
    private readonly Queue<StatusMetricSnapshot> _metricHistory = [];
    private List<EventFeedEntry> _eventFeedAll = [];
    private bool _isOrbiting;
    private Point _lastMousePoint;
    private bool _isUpdatingSliders;
    private bool _hasLoaded;
    private bool _isRealtimeStarted;
    private const int MetricHistoryWindow = 7;

    public MainWindow(
        StarmapScene scene,
        DesktopSession session,
        NavigationApiClient navigationApiClient,
        EconomyApiClient economyApiClient,
        MarketApiClient marketApiClient,
        FleetApiClient fleetApiClient,
        ReputationApiClient reputationApiClient,
        LeaderboardApiClient leaderboardApiClient,
        StrategicApiClient strategicApiClient,
        TelemetryApiClient telemetryApiClient,
        MarketIntelligenceApiClient marketIntelligenceApiClient,
        NpcApiClient npcApiClient,
        CombatApiClient combatApiClient,
        CommunicationApiClient communicationApiClient,
        StrategicRealtimeStreamClient strategicRealtimeClient,
        CommunicationRealtimeStreamClient communicationRealtimeClient)
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
        _strategicRealtimeClient = strategicRealtimeClient;
        _communicationRealtimeClient = communicationRealtimeClient;
        _hotkeyBindings = DesktopHotkeyBindings.FromPreferences(new DesktopPreferencesStore().Load());

        InitializeComponent();
        UpdateBreadcrumb();
        BuildStarmap(_scene);
        DashboardHost.Content = new DashboardPanel(
            _session,
            _scene,
            _navigationApiClient,
            _marketApiClient,
            _fleetApiClient,
            _reputationApiClient,
            _strategicApiClient,
            telemetryApiClient);
        TradingHost.Content = new TradingPanel(
            _session,
            economyApiClient,
            _marketApiClient,
            marketIntelligenceApiClient,
            _npcApiClient);
        RoutesHost.Content = new RoutePlanningPanel(_navigationApiClient);
        BattlesHost.Content = new BattlePanel(_combatApiClient);
        FleetHost.Content = new FleetPanel(_session, fleetApiClient, _marketApiClient);
        IntelHost.Content = new IntelPanel(_session, navigationApiClient, reputationApiClient, strategicApiClient);
        MarketIntelHost.Content = new MarketIntelligencePanel(marketIntelligenceApiClient);
        ServicesHost.Content = new ServicesPanel(_npcApiClient);
        CommunicationHost.Content = new CommunicationPanel(_session, communicationApiClient);
        ReputationHost.Content = new ReputationPanel(_session, _reputationApiClient, leaderboardApiClient);
        TerritoryHost.Content = new TerritoryPanel(_strategicApiClient);
        AnalyticsHost.Content = new AnalyticsPanel(
            _session,
            _marketApiClient,
            _combatApiClient,
            _fleetApiClient,
            marketIntelligenceApiClient,
            _reputationApiClient,
            leaderboardApiClient);
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
        Closed += OnMainWindowClosed;
    }

    private void BuildStarmap(StarmapScene scene)
    {
        RouteList.ItemsSource = scene.Routes;
        SceneModels.Content = scene.Models;
        if (scene.Routes.Count > 0)
        {
            RouteList.SelectedIndex = 0;
            RouteTelemetryText.Text = StarmapRouteTelemetryFormatter.Build(scene.Routes[0]);
        }
        else
        {
            RouteTelemetryText.Text = "No routes available.";
        }
    }

    private void ApplyCamera(bool updateSliders)
    {
        if (SceneCamera is null)
        {
            return;
        }

        var pose = _cameraController.BuildPose();
        SceneCamera.Position = pose.Position;
        SceneCamera.LookDirection = pose.LookDirection;
        SceneCamera.UpDirection = pose.UpDirection;

        if (!updateSliders)
        {
            return;
        }

        if (YawSlider is null || PitchSlider is null || ZoomSlider is null)
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

        if (YawSlider is null || PitchSlider is null || ZoomSlider is null)
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
            RouteTelemetryText.Text = "Select a route to view risk overlay telemetry.";
            return;
        }

        RouteTelemetryText.Text = StarmapRouteTelemetryFormatter.Build(selected);
        _cameraController.FocusOnRoute(selected);
        ApplyCamera(updateSliders: true);
    }

    private void OnModuleTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != ModuleTabs)
        {
            return;
        }

        UpdateBreadcrumb();
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await RefreshDashboardAndEventsAsync();
        StartRealtimeFeeds();
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
            AlertsMetricText.Text = "Alerts: event feed exported";
            Log.Information("Event feed exported to {Path}.", path);
        }
        catch (Exception exception)
        {
            AlertsMetricText.Text = "Alerts: export failed";
            Log.Error(exception, "Event feed export failed.");
            BreakIfDebugging();
        }
    }

    private void OnMainWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifiers = Keyboard.Modifiers;
        if (ModuleHotkeyNavigator.TryResolveTabIndex(key, modifiers, ModuleTabs.Items.Count, out var tabIndex))
        {
            ModuleTabs.SelectedIndex = tabIndex;
            e.Handled = true;
            return;
        }

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
            var shipsTask = _fleetApiClient.GetPlayerShipsAsync(_session.PlayerId);
            var routesTask = _navigationApiClient.GetDangerousRoutesAsync(65);
            var reportsTask = _strategicApiClient.GetIntelligenceReportsAsync(_session.PlayerId);
            var territoryTask = _strategicApiClient.GetTerritoryDominanceAsync();
            var serviceAgentsTask = _npcApiClient.GetAgentsAsync();
            var combatLogsTask = _combatApiClient.GetRecentLogsAsync(limit: 25);
            await Task.WhenAll(transactionsTask, standingsTask, escortTask, shipsTask, routesTask, reportsTask, territoryTask, serviceAgentsTask, combatLogsTask);

            var transactions = transactionsTask.Result;
            var standings = standingsTask.Result;
            var escort = escortTask.Result;
            var ships = shipsTask.Result;
            var routes = routesTask.Result;
            var reports = reportsTask.Result;
            var territory = territoryTask.Result;
            var serviceAgents = serviceAgentsTask.Result;
            var combatLogs = combatLogsTask.Result;

            var threats = ThreatAlertRanker.Build(routes, reports);
            var metrics = StatusMetricAggregator.Build(transactions, standings, escort, threats, ships, _scene);
            ApplyMetrics(metrics);

            _eventFeedAll = EventFeedBuilder.Build(transactions, combatLogs, reports, standings, territory, serviceAgents, DateTime.UtcNow).ToList();
            ApplyEventFilter();
        }
        catch (Exception exception)
        {
            AlertsMetricText.Text = $"Alerts: error";
            Log.Error(exception, "Dashboard refresh failed.");
            BreakIfDebugging();
        }
        finally
        {
            SetDashboardBusy(false);
        }
    }

    private void ApplyEventFilter()
    {
        if (!IsInitialized ||
            EventCategoryFilter is null ||
            EventKeywordFilter is null ||
            EventTimeWindowFilter is null)
        {
            return;
        }

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

    private void StartRealtimeFeeds()
    {
        if (_isRealtimeStarted)
        {
            return;
        }

        _isRealtimeStarted = true;
        _strategicRealtimeClient.SnapshotReceived += OnStrategicSnapshotReceived;
        _communicationRealtimeClient.MessageReceived += OnCommunicationMessageReceived;
        _strategicRealtimeClient.Start(_session.PlayerId);
        _communicationRealtimeClient.Start(_session.PlayerId);
    }

    private async void OnMainWindowClosed(object? sender, EventArgs e)
    {
        _strategicRealtimeClient.SnapshotReceived -= OnStrategicSnapshotReceived;
        _communicationRealtimeClient.MessageReceived -= OnCommunicationMessageReceived;
        await _strategicRealtimeClient.StopAsync();
        await _communicationRealtimeClient.StopAsync();
    }

    private void OnStrategicSnapshotReceived(DashboardRealtimeSnapshotApiDto snapshot)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            var projection = DashboardRealtimeMessageProjector.ApplySnapshot(_eventFeedAll, snapshot);
            ApplyMetrics(projection.Metrics);
            _eventFeedAll = projection.Events.ToList();
            ApplyEventFilter();
        });
    }

    private void OnCommunicationMessageReceived(CommunicationRealtimeMessageApiDto message)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            var communicationEvent = CommunicationEventProjector.Project(message);
            _eventFeedAll = DashboardRealtimeMessageProjector.AppendEvent(_eventFeedAll, communicationEvent).ToList();
            ApplyEventFilter();
        });
    }

    private void ApplyMetrics(StatusMetricSnapshot metrics)
    {
        TrackMetricSnapshot(metrics);
        CreditsMetricText.Text = $"Credits: {metrics.LiquidCredits:N2}";
        NetWorthMetricText.Text = $"Net Worth: {metrics.NetWorth:N2}";
        ReputationMetricText.Text = $"Reputation: {metrics.ReputationScore}";
        FleetMetricText.Text = $"Fleet: {metrics.FleetStrength}";
        ProtectionMetricText.Text = $"Protection: {metrics.ProtectionStatus}";
        RoutesMetricText.Text = $"Routes: {metrics.ActiveRoutes}";
        EconomicIndexMetricText.Text = $"GEI: {metrics.GlobalEconomicIndex:N1}";
        AlertsMetricText.Text = $"Alerts: {metrics.AlertCount}";
        UpdateMetricTooltips();
    }

    private void TrackMetricSnapshot(StatusMetricSnapshot metrics)
    {
        _metricHistory.Enqueue(metrics);
        while (_metricHistory.Count > MetricHistoryWindow)
        {
            _metricHistory.Dequeue();
        }
    }

    private void UpdateMetricTooltips()
    {
        if (_metricHistory.Count == 0)
        {
            return;
        }

        var snapshots = _metricHistory.ToArray();
        CreditsMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Liquid Credits", snapshots.Select(static snapshot => snapshot.LiquidCredits).ToArray(), "N2");
        NetWorthMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Net Worth", snapshots.Select(static snapshot => snapshot.NetWorth).ToArray(), "N2");
        ReputationMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Reputation Score", snapshots.Select(static snapshot => (decimal)snapshot.ReputationScore).ToArray(), "N0");
        FleetMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Fleet Strength", snapshots.Select(static snapshot => (decimal)snapshot.FleetStrength).ToArray(), "N0");
        ProtectionMetricText.ToolTip = TopStatusTooltipBuilder.BuildLabelTrend("Protection Status", snapshots.Select(static snapshot => snapshot.ProtectionStatus).ToArray());
        RoutesMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Active Routes", snapshots.Select(static snapshot => (decimal)snapshot.ActiveRoutes).ToArray(), "N0");
        EconomicIndexMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Global Economic Index", snapshots.Select(static snapshot => snapshot.GlobalEconomicIndex).ToArray(), "N1");
        AlertsMetricText.ToolTip = TopStatusTooltipBuilder.BuildNumeric("Alerts", snapshots.Select(static snapshot => (decimal)snapshot.AlertCount).ToArray(), "N0");
    }

    private void SetDashboardBusy(bool isBusy)
    {
        RefreshDashboardButton.IsEnabled = !isBusy;
        RefreshEventFeedButton.IsEnabled = !isBusy;
    }

    private void UpdateBreadcrumb()
    {
        var selectedModule = (ModuleTabs.SelectedItem as TabItem)?.Header?.ToString();
        BreadcrumbText.Text = ModuleBreadcrumbBuilder.Build(selectedModule);
        QuickActionsText.Text = ModuleQuickActionsBuilder.Build(selectedModule);
        ContextSubmenuList.ItemsSource = ModuleContextSubmenuBuilder.Build(selectedModule);
    }

    private static void BreakIfDebugging()
    {
        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}
