using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class ServicesPanel : UserControl
{
    private readonly NpcApiClient _npcApiClient;
    private readonly ObservableCollection<NpcAgentApiDto> _rows = [];
    private bool _hasLoaded;

    public ServicesPanel(NpcApiClient npcApiClient)
    {
        _npcApiClient = npcApiClient;
        InitializeComponent();
        AgentsGrid.ItemsSource = _rows;
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
            var agents = await _npcApiClient.GetAgentsAsync();
            _rows.Clear();
            foreach (var agent in agents.OrderByDescending(static agent => agent.InfluenceScore))
            {
                _rows.Add(agent);
            }

            SetStatus($"Loaded {_rows.Count} NPC agents.", isError: false);
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
