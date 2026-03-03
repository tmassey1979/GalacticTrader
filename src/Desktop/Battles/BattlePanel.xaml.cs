using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Battles;

public partial class BattlePanel : UserControl
{
    private readonly CombatApiClient _combatApiClient;
    private readonly ObservableCollection<BattleLogDisplayRow> _rows = [];
    private bool _hasLoaded;

    public BattlePanel(CombatApiClient combatApiClient)
    {
        _combatApiClient = combatApiClient;

        InitializeComponent();
        BattleGrid.ItemsSource = _rows;
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
            var logs = await _combatApiClient.GetRecentLogsAsync(limit: 40);
            var rows = BattleFeedBuilder.Build(logs);

            _rows.Clear();
            foreach (var row in rows)
            {
                _rows.Add(row);
            }

            SetStatus($"Loaded {_rows.Count} combat outcomes.", isError: false);
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
