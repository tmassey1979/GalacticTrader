using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class CommunicationPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly CommunicationApiClient _communicationApiClient;
    private readonly ObservableCollection<CommunicationMessageDisplayRow> _rows = [];
    private bool _hasLoaded;

    public CommunicationPanel(DesktopSession session, CommunicationApiClient communicationApiClient)
    {
        _session = session;
        _communicationApiClient = communicationApiClient;
        InitializeComponent();
        MessagesGrid.ItemsSource = _rows;
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

    private async void OnSendClick(object sender, RoutedEventArgs e)
    {
        await SendAsync();
    }

    private async void OnMessageTextKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        e.Handled = true;
        await SendAsync();
    }

    private async Task RefreshAsync()
    {
        var channelType = ResolveChannelType();
        var channelKey = CommunicationChannelKeyNormalizer.Normalize(ChannelKeyText.Text);

        SetBusy(true);
        try
        {
            await _communicationApiClient.SubscribeAsync(new SubscribeChannelApiRequest
            {
                PlayerId = _session.PlayerId,
                ChannelType = ResolveChannelTypeCode(channelType),
                ChannelKey = channelKey
            });

            var messages = await _communicationApiClient.GetRecentMessagesAsync(
                channelType: channelType,
                channelKey: channelKey,
                limit: 100);

            var projected = CommunicationMessageProjector.Build(messages);
            _rows.Clear();
            foreach (var row in projected)
            {
                _rows.Add(row);
            }

            SetStatus($"Loaded {_rows.Count} messages for {channelType}:{channelKey}.", isError: false);
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

    private async Task SendAsync()
    {
        var content = MessageText.Text.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            SetStatus("Enter a message before sending.", isError: true);
            return;
        }

        var channelType = ResolveChannelType();
        var channelKey = CommunicationChannelKeyNormalizer.Normalize(ChannelKeyText.Text);

        SetBusy(true);
        try
        {
            await _communicationApiClient.SendMessageAsync(new SendChannelMessageApiRequest
            {
                PlayerId = _session.PlayerId,
                ChannelType = ResolveChannelTypeCode(channelType),
                ChannelKey = channelKey,
                Content = content
            });

            MessageText.Text = string.Empty;
            await RefreshAsync();
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

    private string ResolveChannelType()
    {
        var selection = (ChannelTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return string.IsNullOrWhiteSpace(selection) ? "Global" : selection;
    }

    private static int ResolveChannelTypeCode(string channelType)
    {
        return channelType.ToLowerInvariant() switch
        {
            "global" => 0,
            "sector" => 1,
            "faction" => 2,
            "private" => 3,
            "fleet" => 4,
            _ => 0
        };
    }

    private void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;
        SendButton.IsEnabled = !isBusy;
        ChannelTypeCombo.IsEnabled = !isBusy;
        ChannelKeyText.IsEnabled = !isBusy;
        MessageText.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
