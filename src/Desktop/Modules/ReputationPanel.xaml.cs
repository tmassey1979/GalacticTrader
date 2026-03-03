using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class ReputationPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly ReputationApiClient _reputationApiClient;
    private readonly ObservableCollection<PlayerFactionStandingApiDto> _rows = [];
    private bool _hasLoaded;

    public ReputationPanel(DesktopSession session, ReputationApiClient reputationApiClient)
    {
        _session = session;
        _reputationApiClient = reputationApiClient;
        InitializeComponent();
        ReputationGrid.ItemsSource = _rows;
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
            var standings = await _reputationApiClient.GetFactionStandingsAsync(_session.PlayerId);
            _rows.Clear();
            foreach (var standing in standings.OrderByDescending(static standing => standing.ReputationScore))
            {
                _rows.Add(standing);
            }

            SetStatus($"Loaded {_rows.Count} faction standings.", isError: false);
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
