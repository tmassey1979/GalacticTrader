using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Starmap;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Dashboard;

public partial class DashboardPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly StarmapScene _scene;
    private readonly NavigationApiClient _navigationApiClient;
    private readonly MarketApiClient _marketApiClient;
    private readonly FleetApiClient _fleetApiClient;
    private readonly ReputationApiClient _reputationApiClient;
    private readonly StrategicApiClient _strategicApiClient;
    private readonly TelemetryApiClient _telemetryApiClient;
    private bool _hasLoaded;

    public DashboardPanel(
        DesktopSession session,
        StarmapScene scene,
        NavigationApiClient navigationApiClient,
        MarketApiClient marketApiClient,
        FleetApiClient fleetApiClient,
        ReputationApiClient reputationApiClient,
        StrategicApiClient strategicApiClient,
        TelemetryApiClient telemetryApiClient)
    {
        _session = session;
        _scene = scene;
        _navigationApiClient = navigationApiClient;
        _marketApiClient = marketApiClient;
        _fleetApiClient = fleetApiClient;
        _reputationApiClient = reputationApiClient;
        _strategicApiClient = strategicApiClient;
        _telemetryApiClient = telemetryApiClient;

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await RefreshSummaryAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshSummaryAsync();
    }

    private async Task RefreshSummaryAsync()
    {
        RefreshButton.IsEnabled = false;
        try
        {
            var transactionsTask = _marketApiClient.GetTransactionsAsync(_session.PlayerId, limit: 25);
            var shipsTask = _fleetApiClient.GetPlayerShipsAsync(_session.PlayerId);
            var escortTask = _fleetApiClient.GetEscortSummaryAsync(_session.PlayerId);
            var standingsTask = _reputationApiClient.GetFactionStandingsAsync(_session.PlayerId);
            var dangerousRoutesTask = _navigationApiClient.GetDangerousRoutesAsync(65);
            var reportsTask = _strategicApiClient.GetIntelligenceReportsAsync(_session.PlayerId);
            var globalSummaryTask = _telemetryApiClient.GetGlobalSummaryAsync();
            await Task.WhenAll(transactionsTask, shipsTask, escortTask, standingsTask, dangerousRoutesTask, reportsTask, globalSummaryTask);

            var summary = DashboardSummaryBuilder.Build(
                transactionsTask.Result,
                shipsTask.Result,
                escortTask.Result,
                standingsTask.Result,
                dangerousRoutesTask.Result,
                reportsTask.Result,
                _scene);

            CreditsValue.Text = $"Credits {summary.LiquidCredits:N2}";
            TradeVolumeValue.Text = $"Recent volume {summary.RecentTradeVolume:N2}";
            FleetValue.Text = $"Strength {summary.FleetStrength}";
            ShipCountValue.Text = $"Ships {summary.ShipCount}";
            ReputationValue.Text = $"Peak rep {summary.HighestReputation}";
            FactionAccessValue.Text = $"Faction access {summary.AccessibleFactions}";
            RoutesValue.Text = $"Routes {summary.TotalRoutes}";
            RiskRoutesValue.Text = $"High risk {summary.HighRiskRoutes}";
            IntelReportsValue.Text = summary.IntelligenceReports.ToString();
            ThreatsValue.Text = summary.ThreatAlerts.ToString();

            var globalMetrics = DashboardGlobalMetricsBuilder.Build(globalSummaryTask.Result);
            GlobalUsersValue.Text = globalMetrics.TotalUsers.ToString("N0");
            GlobalActivePlayersValue.Text = globalMetrics.ActivePlayers24h.ToString("N0");
            GlobalBattlesPerHourValue.Text = globalMetrics.AvgBattlesPerHour.ToString("N2");
            GlobalEconomicStabilityValue.Text = globalMetrics.EconomicStabilityIndex.ToString("N1");
            GlobalTopReputationValue.Text = globalMetrics.TopReputationPlayerDisplay;
            GlobalTopFinancialValue.Text = globalMetrics.TopFinancialPlayerDisplay;
            SetStatus("Strategic dashboard refreshed.", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
