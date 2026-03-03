using GalacticTrader.Desktop.Api;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class AnalyticsPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly MarketApiClient _marketApiClient;
    private readonly CombatApiClient _combatApiClient;
    private IReadOnlyList<TradeExecutionResultApiDto> _latestTrades = [];
    private IReadOnlyList<CombatLogApiDto> _latestCombats = [];
    private bool _hasLoaded;

    public AnalyticsPanel(
        DesktopSession session,
        MarketApiClient marketApiClient,
        CombatApiClient combatApiClient)
    {
        _session = session;
        _marketApiClient = marketApiClient;
        _combatApiClient = combatApiClient;
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
        await RefreshAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var csv = AnalyticsCsvExporter.BuildCsv(_latestTrades, _latestCombats);
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GalacticTrader");
            Directory.CreateDirectory(root);
            var filename = $"analytics-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            var path = Path.Combine(root, filename);
            File.WriteAllText(path, csv);
            SetStatus($"Analytics exported: {path}", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
    }

    private async Task RefreshAsync()
    {
        RefreshButton.IsEnabled = false;
        ExportButton.IsEnabled = false;
        try
        {
            var tradesTask = _marketApiClient.GetTransactionsAsync(_session.PlayerId, limit: 60);
            var combatsTask = _combatApiClient.GetRecentLogsAsync(limit: 60);
            await Task.WhenAll(tradesTask, combatsTask);

            _latestTrades = tradesTask.Result;
            _latestCombats = combatsTask.Result;
            var snapshot = AnalyticsSnapshotBuilder.Build(_latestTrades, _latestCombats);
            RevenueText.Text = snapshot.RevenueVolume.ToString("N2");
            TradesText.Text = snapshot.TradeCount.ToString();
            AvgTradeText.Text = snapshot.AverageTradeSize.ToString("N2");
            CombatsText.Text = snapshot.CombatCount.ToString();
            AvgDurationText.Text = $"{snapshot.AverageCombatDurationSeconds}s";
            InsuranceText.Text = snapshot.InsurancePayoutTotal.ToString("N2");
            TrendBars.ItemsSource = AnalyticsTrendBuilder.BuildRevenueBars(_latestTrades);
            SetStatus("Analytics refreshed.", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            ExportButton.IsEnabled = true;
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
