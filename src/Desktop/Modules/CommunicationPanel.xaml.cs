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
    private Guid? _activeVoiceChannelId;
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

    private async void OnCreateVoiceClick(object sender, RoutedEventArgs e)
    {
        await CreateVoiceChannelAsync();
    }

    private async void OnJoinVoiceClick(object sender, RoutedEventArgs e)
    {
        await JoinVoiceChannelAsync();
    }

    private async void OnLeaveVoiceClick(object sender, RoutedEventArgs e)
    {
        await LeaveVoiceChannelAsync();
    }

    private async void OnRefreshVoiceQosClick(object sender, RoutedEventArgs e)
    {
        await RefreshVoiceQosAsync();
    }

    private async void OnReportVoiceActivityClick(object sender, RoutedEventArgs e)
    {
        await ReportVoiceActivityAsync();
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

    private async Task CreateVoiceChannelAsync()
    {
        SetBusy(true);
        try
        {
            var created = await _communicationApiClient.CreateVoiceChannelAsync(new CreateVoiceChannelApiRequest
            {
                CreatorPlayerId = _session.PlayerId,
                Mode = ResolveVoiceModeCode(),
                ScopeKey = ResolveVoiceScopeKey()
            });

            _activeVoiceChannelId = created.ChannelId;
            VoiceChannelIdText.Text = created.ChannelId.ToString("D");
            UpdateVoiceChannelFields(created, qos: null);
            SetStatus($"Voice channel created: {created.ChannelId:D}", isError: false);
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

    private async Task JoinVoiceChannelAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to join.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var joined = await _communicationApiClient.JoinVoiceChannelAsync(channelId, new JoinVoiceChannelApiRequest
            {
                PlayerId = _session.PlayerId
            });

            if (joined is null)
            {
                SetStatus("Voice channel not found.", isError: true);
                return;
            }

            _activeVoiceChannelId = joined.ChannelId;
            VoiceChannelIdText.Text = joined.ChannelId.ToString("D");
            UpdateVoiceChannelFields(joined, qos: null);
            SetStatus($"Joined voice channel {joined.ChannelId:D}.", isError: false);
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

    private async Task LeaveVoiceChannelAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to leave.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var left = await _communicationApiClient.LeaveVoiceChannelAsync(channelId, _session.PlayerId);
            if (!left)
            {
                SetStatus("Voice channel not found.", isError: true);
                return;
            }

            _activeVoiceChannelId = null;
            VoiceChannelText.Text = "Channel: -";
            VoiceParticipantsText.Text = "Participants: -";
            VoiceQosText.Text = "QoS: No QoS sample.";
            SetStatus($"Left voice channel {channelId:D}.", isError: false);
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

    private async Task RefreshVoiceQosAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to fetch QoS.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var qos = await _communicationApiClient.GetVoiceQosSnapshotAsync(channelId);
            if (qos is null)
            {
                SetStatus("Voice channel not found.", isError: true);
                return;
            }

            _activeVoiceChannelId = channelId;
            VoiceChannelText.Text = $"Channel: {channelId:D}";
            VoiceParticipantsText.Text = $"Participants: {qos.ParticipantCount}";
            VoiceQosText.Text = $"QoS: {VoiceQosSummaryFormatter.Build(qos)}";
            SetStatus($"Voice QoS updated for {channelId:D}.", isError: false);
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

    private async Task ReportVoiceActivityAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to report activity.", isError: true);
            return;
        }

        if (!VoiceActivityInputParser.TryParseRms(VoiceRmsText.Text, out var rmsLevel))
        {
            SetStatus("RMS must be a number between 0 and 1.", isError: true);
            return;
        }

        if (!VoiceActivityInputParser.TryParsePercent(VoiceLossText.Text, out var packetLossPercent))
        {
            SetStatus("Packet loss must be a numeric percent.", isError: true);
            return;
        }

        if (!VoiceActivityInputParser.TryParseMs(VoiceLatencyText.Text, out var latencyMs))
        {
            SetStatus("Latency must be a numeric millisecond value.", isError: true);
            return;
        }

        if (!VoiceActivityInputParser.TryParseMs(VoiceJitterText.Text, out var jitterMs))
        {
            SetStatus("Jitter must be a numeric millisecond value.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var activity = await _communicationApiClient.UpdateVoiceActivityAsync(channelId, new VoiceActivityApiRequest
            {
                PlayerId = _session.PlayerId,
                RmsLevel = rmsLevel,
                PacketLossPercent = packetLossPercent,
                LatencyMs = latencyMs,
                JitterMs = jitterMs
            });

            if (activity is null)
            {
                SetStatus("Voice channel not found.", isError: true);
                return;
            }

            var qos = await _communicationApiClient.GetVoiceQosSnapshotAsync(channelId);
            if (qos is not null)
            {
                VoiceChannelText.Text = $"Channel: {channelId:D}";
                VoiceParticipantsText.Text = $"Participants: {qos.ParticipantCount}";
                VoiceQosText.Text = $"QoS: {VoiceQosSummaryFormatter.Build(qos)}";
            }

            SetStatus($"Voice activity reported. Score {activity.VoiceActivityScore:N2}.", isError: false);
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

    private int ResolveVoiceModeCode()
    {
        var selection = (VoiceModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return selection switch
        {
            "Fleet" => 1,
            "EncryptedPrivate" => 2,
            _ => 0
        };
    }

    private string ResolveVoiceScopeKey()
    {
        return string.IsNullOrWhiteSpace(VoiceScopeKeyText.Text)
            ? "default"
            : VoiceScopeKeyText.Text.Trim().ToLowerInvariant();
    }

    private bool TryResolveVoiceChannelId(out Guid channelId)
    {
        if (Guid.TryParse(VoiceChannelIdText.Text.Trim(), out channelId))
        {
            return true;
        }

        if (_activeVoiceChannelId.HasValue)
        {
            channelId = _activeVoiceChannelId.Value;
            return true;
        }

        channelId = Guid.Empty;
        return false;
    }

    private void UpdateVoiceChannelFields(VoiceChannelApiDto channel, VoiceQosSnapshotApiDto? qos)
    {
        VoiceChannelText.Text = $"Channel: {channel.ChannelId:D} | Mode {channel.Mode} | Scope {channel.ScopeKey}";
        VoiceParticipantsText.Text = $"Participants: {channel.ParticipantCount}";
        VoiceQosText.Text = $"QoS: {VoiceQosSummaryFormatter.Build(qos)}";
    }

    private void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;
        SendButton.IsEnabled = !isBusy;
        ChannelTypeCombo.IsEnabled = !isBusy;
        ChannelKeyText.IsEnabled = !isBusy;
        MessageText.IsEnabled = !isBusy;
        CreateVoiceButton.IsEnabled = !isBusy;
        JoinVoiceButton.IsEnabled = !isBusy;
        LeaveVoiceButton.IsEnabled = !isBusy;
        RefreshVoiceQosButton.IsEnabled = !isBusy;
        VoiceModeCombo.IsEnabled = !isBusy;
        VoiceScopeKeyText.IsEnabled = !isBusy;
        VoiceChannelIdText.IsEnabled = !isBusy;
        VoiceRmsText.IsEnabled = !isBusy;
        VoiceLossText.IsEnabled = !isBusy;
        VoiceLatencyText.IsEnabled = !isBusy;
        VoiceJitterText.IsEnabled = !isBusy;
        ReportVoiceActivityButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
