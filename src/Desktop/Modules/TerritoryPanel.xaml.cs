using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class TerritoryPanel : UserControl
{
    private readonly StrategicApiClient _strategicApiClient;
    private readonly ObservableCollection<TerritoryDominanceApiDto> _rows = [];
    private bool _hasLoaded;

    public TerritoryPanel(StrategicApiClient strategicApiClient)
    {
        _strategicApiClient = strategicApiClient;
        InitializeComponent();
        TerritoryGrid.ItemsSource = _rows;
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
        RefreshButton.IsEnabled = false;
        try
        {
            var dominance = await _strategicApiClient.GetTerritoryDominanceAsync();
            _rows.Clear();
            foreach (var row in dominance.OrderByDescending(static item => item.DominanceScore))
            {
                _rows.Add(row);
            }

            SetStatus($"Loaded {_rows.Count} territory records.", isError: false);
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
