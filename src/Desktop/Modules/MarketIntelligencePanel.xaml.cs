using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class MarketIntelligencePanel : UserControl
{
    private readonly MarketIntelligenceApiClient _marketIntelligenceApiClient;
    private readonly ObservableCollection<MarketHeatmapDisplayRow> _heatmapRows = [];
    private readonly ObservableCollection<MarketTraderDisplayRow> _traderRows = [];
    private readonly ObservableCollection<SmugglingCorridorDisplayRow> _corridorRows = [];
    private bool _hasLoaded;

    public MarketIntelligencePanel(MarketIntelligenceApiClient marketIntelligenceApiClient)
    {
        _marketIntelligenceApiClient = marketIntelligenceApiClient;
        InitializeComponent();
        HeatmapGrid.ItemsSource = _heatmapRows;
        TopTradersGrid.ItemsSource = _traderRows;
        CorridorsGrid.ItemsSource = _corridorRows;
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

    private async Task RefreshAsync()
    {
        SetBusy(true);
        try
        {
            var summary = await _marketIntelligenceApiClient.GetSummaryAsync();
            var snapshot = MarketIntelligenceProjection.Build(summary);

            VolatilityValue.Text = $"{snapshot.VolatilityIndex:N1}";
            ReplaceRows(_heatmapRows, snapshot.Heatmap);
            ReplaceRows(_traderRows, snapshot.TopTraders);
            ReplaceRows(_corridorRows, snapshot.SmugglingCorridors);

            SetStatus("Market intelligence refreshed.", isError: false);
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

    private static void ReplaceRows<TRow>(ObservableCollection<TRow> target, IReadOnlyList<TRow> source)
    {
        target.Clear();
        foreach (var row in source)
        {
            target.Add(row);
        }
    }
}
