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
    private readonly ObservableCollection<ReputationStandingDisplayRow> _rows = [];
    private readonly ObservableCollection<string> _benefits = [];
    private bool _hasLoaded;

    public ReputationPanel(DesktopSession session, ReputationApiClient reputationApiClient)
    {
        _session = session;
        _reputationApiClient = reputationApiClient;
        InitializeComponent();
        ReputationGrid.ItemsSource = _rows;
        BenefitsList.ItemsSource = _benefits;
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

    private async void OnApplyActionClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ActionMagnitudeText.Text.Trim(), out var magnitude) || magnitude <= 0)
        {
            SetStatus("Action magnitude must be a positive integer.", isError: true);
            return;
        }

        var actionType = (AlignmentActionCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "LegalTrade";

        SetBusy(true);
        try
        {
            var state = await _reputationApiClient.ApplyAlignmentActionAsync(new AlignmentActionApiRequest
            {
                PlayerId = _session.PlayerId,
                ActionType = actionType,
                Magnitude = magnitude
            });

            if (state is null)
            {
                SetStatus("Alignment action target player was not found.", isError: true);
                return;
            }

            await RefreshAlignmentAccessAsync();
            SetStatus($"Applied {actionType} x{magnitude}. Alignment level: {state.AlignmentLevel}.", isError: false);
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

    private async Task RefreshAsync()
    {
        SetBusy(true);
        try
        {
            var standingsTask = _reputationApiClient.GetFactionStandingsAsync(_session.PlayerId);
            var benefitsTask = _reputationApiClient.GetFactionBenefitsAsync(_session.PlayerId);
            var accessTask = _reputationApiClient.GetAlignmentAccessAsync(_session.PlayerId);
            await Task.WhenAll(standingsTask, benefitsTask, accessTask);

            var standings = standingsTask.Result;
            var benefits = benefitsTask.Result;
            var access = accessTask.Result;

            _rows.Clear();
            foreach (var standing in standings.OrderByDescending(static standing => standing.ReputationScore))
            {
                var badge = ReputationBadgeProjector.Build(standing);
                _rows.Add(new ReputationStandingDisplayRow
                {
                    FactionId = standing.FactionId.ToString()[..8],
                    ReputationScore = standing.ReputationScore,
                    Tier = standing.Tier,
                    Badge = badge.Badge,
                    AccentHex = badge.AccentHex,
                    HasAccess = standing.HasAccess,
                    TradingDiscount = standing.TradingDiscount
                });
            }

            _benefits.Clear();
            foreach (var benefit in benefits)
            {
                var summary = benefit.Benefits.Count == 0
                    ? $"{benefit.FactionName} [{benefit.Tier}]"
                    : $"{benefit.FactionName} [{benefit.Tier}] - {string.Join(", ", benefit.Benefits)}";
                _benefits.Add(summary);
            }

            if (access is null)
            {
                AlignmentAccessText.Text = "No alignment profile available.";
            }
            else
            {
                AlignmentAccessText.Text =
                    $"Path {access.Path} | Level {access.AlignmentLevel}\n" +
                    $"Legal Insurance: {access.CanUseLegalInsurance} | Black Market: {access.CanAccessBlackMarket}\n" +
                    $"Scan Mod: {access.ScanFrequencyModifier:P1} | Insurance Mod: {access.InsuranceCostModifier:P1}";
            }

            SetStatus($"Loaded {_rows.Count} faction standings.", isError: false);
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

    private async Task RefreshAlignmentAccessAsync()
    {
        var access = await _reputationApiClient.GetAlignmentAccessAsync(_session.PlayerId);
        AlignmentAccessText.Text = access is null
            ? "No alignment profile available."
            : $"Path {access.Path} | Level {access.AlignmentLevel}\n" +
              $"Legal Insurance: {access.CanUseLegalInsurance} | Black Market: {access.CanAccessBlackMarket}\n" +
              $"Scan Mod: {access.ScanFrequencyModifier:P1} | Insurance Mod: {access.InsuranceCostModifier:P1}";
    }

    private void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;
        ApplyActionButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
