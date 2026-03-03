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
    private readonly ObservableCollection<string> _voiceSignalLog = [];
    private readonly ObservableCollection<string> _spatialMixLog = [];
    private int? _currentChannelTypeCode;
    private string? _currentChannelKey;
    private Guid? _activeVoiceChannelId;
    private bool _hasLoaded;

    public CommunicationPanel(DesktopSession session, CommunicationApiClient communicationApiClient)
    {
        _session = session;
        _communicationApiClient = communicationApiClient;
        InitializeComponent();
        MessagesGrid.ItemsSource = _rows;
        VoiceSignalLogList.ItemsSource = _voiceSignalLog;
        SpatialMixLogList.ItemsSource = _spatialMixLog;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        await CleanupChannelSubscriptionAsync();
        _hasLoaded = false;
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

    private async void OnSendVoiceSignalClick(object sender, RoutedEventArgs e)
    {
        await SendVoiceSignalAsync();
    }

    private async void OnPollVoiceSignalsClick(object sender, RoutedEventArgs e)
    {
        await PollVoiceSignalsAsync();
    }

    private async void OnPreviewSpatialMixClick(object sender, RoutedEventArgs e)
    {
        await PreviewSpatialMixAsync();
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
        var channelTypeCode = ResolveChannelTypeCode(channelType);
        var channelKey = CommunicationChannelKeyNormalizer.Normalize(ChannelKeyText.Text);
        var transition = CommunicationSubscriptionPlanner.Plan(
            _currentChannelTypeCode,
            _currentChannelKey,
            channelTypeCode,
            channelKey);

        SetBusy(true);
        try
        {
            if (transition.ShouldUnsubscribeCurrent &&
                _currentChannelTypeCode.HasValue &&
                !string.IsNullOrWhiteSpace(_currentChannelKey))
            {
                await _communicationApiClient.UnsubscribeAsync(new SubscribeChannelApiRequest
                {
                    PlayerId = _session.PlayerId,
                    ChannelType = _currentChannelTypeCode.Value,
                    ChannelKey = _currentChannelKey
                });

                _currentChannelTypeCode = null;
                _currentChannelKey = null;
            }

            if (transition.ShouldSubscribeNext)
            {
                await _communicationApiClient.SubscribeAsync(new SubscribeChannelApiRequest
                {
                    PlayerId = _session.PlayerId,
                    ChannelType = channelTypeCode,
                    ChannelKey = channelKey
                });

                _currentChannelTypeCode = channelTypeCode;
                _currentChannelKey = channelKey;
            }

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

    private async Task CleanupChannelSubscriptionAsync()
    {
        if (!_currentChannelTypeCode.HasValue || string.IsNullOrWhiteSpace(_currentChannelKey))
        {
            return;
        }

        try
        {
            await _communicationApiClient.UnsubscribeAsync(new SubscribeChannelApiRequest
            {
                PlayerId = _session.PlayerId,
                ChannelType = _currentChannelTypeCode.Value,
                ChannelKey = _currentChannelKey
            });
        }
        catch
        {
            // Best-effort cleanup during panel teardown.
        }
        finally
        {
            _currentChannelTypeCode = null;
            _currentChannelKey = null;
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

    private async Task SendVoiceSignalAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to send a signal.", isError: true);
            return;
        }

        var signalType = VoiceSignalTypeText.Text.Trim();
        if (string.IsNullOrWhiteSpace(signalType))
        {
            SetStatus("Signal type is required.", isError: true);
            return;
        }

        var payload = VoiceSignalPayloadText.Text.Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            SetStatus("Signal payload is required.", isError: true);
            return;
        }

        if (!TryResolveSignalTarget(out var targetPlayerId))
        {
            SetStatus("Target player id must be blank or a valid GUID.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var signal = await _communicationApiClient.PublishVoiceSignalAsync(channelId, new VoiceSignalApiRequest
            {
                SenderId = _session.PlayerId,
                TargetPlayerId = targetPlayerId,
                SignalType = signalType,
                Payload = payload
            });

            if (signal is null)
            {
                SetStatus("Voice channel not found.", isError: true);
                return;
            }

            AppendVoiceSignal(signal);
            SetStatus($"Voice signal sent ({signal.SignalType}).", isError: false);
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

    private async Task PollVoiceSignalsAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to poll signals.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var signals = await _communicationApiClient.DequeueVoiceSignalsAsync(channelId, _session.PlayerId, limit: 25);
            foreach (var signal in signals.OrderBy(static signal => signal.CreatedAt))
            {
                AppendVoiceSignal(signal);
            }

            SetStatus($"Polled {signals.Count} signal(s).", isError: false);
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

    private async Task PreviewSpatialMixAsync()
    {
        if (!TryResolveVoiceChannelId(out var channelId))
        {
            SetStatus("Enter a valid voice channel id to preview spatial audio.", isError: true);
            return;
        }

        if (!Guid.TryParse(SpatialSpeakerIdText.Text.Trim(), out var speakerId))
        {
            SetStatus("Speaker player id must be a valid GUID.", isError: true);
            return;
        }

        if (!TryParseFloat(SpatialXText.Text, out var speakerX) ||
            !TryParseFloat(SpatialYText.Text, out var speakerY) ||
            !TryParseFloat(SpatialZText.Text, out var speakerZ))
        {
            SetStatus("Speaker coordinates must be numeric values.", isError: true);
            return;
        }

        if (!VoiceActivityInputParser.TryParseRms(SpatialGainText.Text, out var baseGain))
        {
            SetStatus("Speaker gain must be numeric (0..1).", isError: true);
            return;
        }

        if (!VoiceActivityInputParser.TryParseMs(SpatialFalloffText.Text, out var falloffDistance))
        {
            SetStatus("Falloff distance must be a numeric value.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _communicationApiClient.CalculateSpatialAudioAsync(channelId, new SpatialAudioApiRequest
            {
                ListenerId = _session.PlayerId,
                ListenerX = 0f,
                ListenerY = 0f,
                ListenerZ = 0f,
                FalloffDistance = Math.Max(falloffDistance, 1f),
                Speakers =
                [
                    new SpeakerSampleApiRequest
                    {
                        PlayerId = speakerId,
                        X = speakerX,
                        Y = speakerY,
                        Z = speakerZ,
                        BaseGain = baseGain
                    }
                ]
            });

            if (result is null)
            {
                SetStatus("Spatial mix unavailable. Ensure you are joined to the voice channel.", isError: true);
                return;
            }

            _spatialMixLog.Clear();
            foreach (var mix in result.Mix.OrderBy(static entry => entry.Distance))
            {
                _spatialMixLog.Add(SpatialAudioMixFormatter.Build(mix));
            }

            if (_spatialMixLog.Count == 0)
            {
                _spatialMixLog.Add("No mix entries.");
            }

            SetStatus($"Spatial mix calculated with {_spatialMixLog.Count} entry(ies).", isError: false);
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

    private bool TryResolveSignalTarget(out Guid? targetPlayerId)
    {
        var value = VoiceSignalTargetText.Text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            targetPlayerId = null;
            return true;
        }

        if (Guid.TryParse(value, out var parsed))
        {
            targetPlayerId = parsed;
            return true;
        }

        targetPlayerId = null;
        return false;
    }

    private void AppendVoiceSignal(VoiceSignalApiDto signal)
    {
        _voiceSignalLog.Insert(0, VoiceSignalLogFormatter.Build(signal));
        while (_voiceSignalLog.Count > 40)
        {
            _voiceSignalLog.RemoveAt(_voiceSignalLog.Count - 1);
        }
    }

    private static bool TryParseFloat(string input, out float value)
    {
        return float.TryParse(input.Trim(), out value);
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
        VoiceSignalTypeText.IsEnabled = !isBusy;
        VoiceSignalTargetText.IsEnabled = !isBusy;
        VoiceSignalPayloadText.IsEnabled = !isBusy;
        SendVoiceSignalButton.IsEnabled = !isBusy;
        PollVoiceSignalsButton.IsEnabled = !isBusy;
        SpatialFalloffText.IsEnabled = !isBusy;
        SpatialSpeakerIdText.IsEnabled = !isBusy;
        SpatialXText.IsEnabled = !isBusy;
        SpatialYText.IsEnabled = !isBusy;
        SpatialZText.IsEnabled = !isBusy;
        SpatialGainText.IsEnabled = !isBusy;
        PreviewSpatialMixButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
