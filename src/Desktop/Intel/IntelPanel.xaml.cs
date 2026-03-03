using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Intel;

public partial class IntelPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly NavigationApiClient _navigationApiClient;
    private readonly ReputationApiClient _reputationApiClient;
    private readonly StrategicApiClient _strategicApiClient;
    private readonly ObservableCollection<FactionStandingDisplayRow> _standingRows = [];
    private readonly ObservableCollection<string> _benefitRows = [];
    private readonly ObservableCollection<ThreatAlert> _threatRows = [];
    private readonly ObservableCollection<IntelligenceReportDisplayRow> _reportRows = [];
    private bool _hasLoaded;

    public IntelPanel(
        DesktopSession session,
        NavigationApiClient navigationApiClient,
        ReputationApiClient reputationApiClient,
        StrategicApiClient strategicApiClient)
    {
        _session = session;
        _navigationApiClient = navigationApiClient;
        _reputationApiClient = reputationApiClient;
        _strategicApiClient = strategicApiClient;

        InitializeComponent();
        StandingsGrid.ItemsSource = _standingRows;
        BenefitsList.ItemsSource = _benefitRows;
        ThreatGrid.ItemsSource = _threatRows;
        ReportsGrid.ItemsSource = _reportRows;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await RefreshIntelAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshIntelAsync();
    }

    private async Task RefreshIntelAsync()
    {
        SetBusy(true);
        try
        {
            var standingsTask = _reputationApiClient.GetFactionStandingsAsync(_session.PlayerId);
            var benefitsTask = _reputationApiClient.GetFactionBenefitsAsync(_session.PlayerId);
            var reportsTask = _strategicApiClient.GetIntelligenceReportsAsync(_session.PlayerId);
            var routesTask = _navigationApiClient.GetDangerousRoutesAsync(65);
            await Task.WhenAll(standingsTask, benefitsTask, reportsTask, routesTask);

            var standings = standingsTask.Result;
            var benefits = benefitsTask.Result;
            var reports = reportsTask.Result;
            var routes = routesTask.Result;

            _standingRows.Clear();
            foreach (var standing in standings.OrderByDescending(static standing => standing.ReputationScore))
            {
                _standingRows.Add(new FactionStandingDisplayRow
                {
                    FactionId = standing.FactionId.ToString()[..8],
                    ReputationScore = standing.ReputationScore,
                    Tier = standing.Tier,
                    Access = standing.HasAccess ? "Yes" : "No",
                    TradingDiscount = standing.TradingDiscount,
                    TaxModifier = standing.TaxModifier
                });
            }

            _benefitRows.Clear();
            foreach (var benefit in benefits)
            {
                var line = benefit.Benefits.Count == 0
                    ? $"{benefit.FactionName} [{benefit.Tier}]"
                    : $"{benefit.FactionName} [{benefit.Tier}] - {string.Join(", ", benefit.Benefits)}";
                _benefitRows.Add(line);
            }

            _reportRows.Clear();
            foreach (var report in reports.OrderByDescending(static report => report.ConfidenceScore).ThenBy(static report => report.SectorName))
            {
                _reportRows.Add(new IntelligenceReportDisplayRow
                {
                    SignalType = report.SignalType,
                    SectorName = report.SectorName,
                    ConfidenceScore = report.ConfidenceScore,
                    Payload = report.Payload,
                    ExpiresAtUtc = report.ExpiresAt.ToUniversalTime().ToString("u")
                });
            }

            _threatRows.Clear();
            foreach (var alert in ThreatAlertRanker.Build(routes, reports))
            {
                _threatRows.Add(alert);
            }

            SetStatus(
                $"Loaded {_standingRows.Count} standings, {_reportRows.Count} reports, {_threatRows.Count} ranked threats.",
                isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
