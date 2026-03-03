using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class TerritoryPanel : UserControl
{
    private readonly StrategicApiClient _strategicApiClient;
    private readonly ObservableCollection<TerritoryDominanceDisplayRow> _rows = [];
    private readonly Dictionary<Guid, string> _protectionPriorities = [];
    private readonly Dictionary<Guid, TerritoryDominanceApiDto> _recordsByFactionId = [];
    private readonly Dictionary<Guid, TerritoryEconomicPolicyApiDto> _economicPoliciesByFactionId = [];
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

    private void OnTerritorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TerritoryGrid.SelectedItem is not TerritoryDominanceDisplayRow selected)
        {
            SelectedTerritoryText.Text = "Select a faction row.";
            return;
        }

        SelectedTerritoryText.Text =
            $"{selected.FactionName} | Dominance {selected.DominanceScore:N1} | Heat {selected.HeatHex}\n" +
            $"Sectors {selected.ControlledSectorCount} | Priority {selected.ProtectionPriority} | Tax {selected.TaxRatePercent:N1}% | Incentive {selected.TradeIncentivePercent:N1}%";

        TaxRateTextBox.Text = selected.TaxRatePercent.ToString("N1");
        TradeIncentiveTextBox.Text = selected.TradeIncentivePercent.ToString("N1");
    }

    private void OnAssignProtectionClick(object sender, RoutedEventArgs e)
    {
        if (TerritoryGrid.SelectedItem is not TerritoryDominanceDisplayRow selected)
        {
            SetStatus("Select a faction row first.", isError: true);
            return;
        }

        var priority = (ProtectionPriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Low";
        _protectionPriorities[selected.FactionId] = priority;
        RebuildRows();
        SetStatus($"Protection priority for {selected.FactionName} set to {priority}.", isError: false);
    }

    private async void OnRecalculateClick(object sender, RoutedEventArgs e)
    {
        if (TerritoryGrid.SelectedItem is not TerritoryDominanceDisplayRow selected)
        {
            SetStatus("Select a faction row first.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var updated = await _strategicApiClient.RecalculateTerritoryDominanceAsync(selected.FactionId);
            if (updated is null)
            {
                SetStatus("Faction was not found for dominance recalculation.", isError: true);
                return;
            }

            _recordsByFactionId[selected.FactionId] = updated;
            RebuildRows();
            SetStatus($"Recalculated dominance for {updated.FactionName}: {updated.DominanceScore:N1}.", isError: false);
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
            var dominance = await _strategicApiClient.GetTerritoryDominanceAsync();
            var policies = await _strategicApiClient.GetTerritoryEconomicPoliciesAsync();
            _recordsByFactionId.Clear();
            foreach (var row in dominance)
            {
                _recordsByFactionId[row.FactionId] = row;
            }

            _economicPoliciesByFactionId.Clear();
            foreach (var policy in policies)
            {
                _economicPoliciesByFactionId[policy.FactionId] = policy;
            }

            RebuildRows();
            SetStatus($"Loaded {_rows.Count} territory records with economic policy overlays.", isError: false);
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

    private void RebuildRows()
    {
        var projected = TerritoryHeatmapProjector.Build(_recordsByFactionId.Values.ToArray(), _protectionPriorities, _economicPoliciesByFactionId);
        _rows.Clear();
        foreach (var row in projected)
        {
            _rows.Add(row);
        }

        if (_rows.Count > 0)
        {
            TerritoryGrid.SelectedIndex = 0;
        }
    }

    private async void OnApplyEconomicPolicyClick(object sender, RoutedEventArgs e)
    {
        if (TerritoryGrid.SelectedItem is not TerritoryDominanceDisplayRow selected)
        {
            SetStatus("Select a faction row first.", isError: true);
            return;
        }

        if (!TerritoryEconomicPolicyInputParser.TryParsePercent(TaxRateTextBox.Text, minimum: 0m, maximum: 50m, out var taxRatePercent))
        {
            SetStatus("Tax rate must be a number between 0 and 50.", isError: true);
            return;
        }

        if (!TerritoryEconomicPolicyInputParser.TryParsePercent(TradeIncentiveTextBox.Text, minimum: -50m, maximum: 50m, out var incentivePercent))
        {
            SetStatus("Trade incentive must be a number between -50 and 50.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            var updated = await _strategicApiClient.UpsertTerritoryEconomicPolicyAsync(new UpsertTerritoryEconomicPolicyApiRequest
            {
                FactionId = selected.FactionId,
                TaxRatePercent = taxRatePercent,
                TradeIncentivePercent = incentivePercent
            });

            if (updated is null)
            {
                SetStatus("Faction was not found for economic policy update.", isError: true);
                return;
            }

            _economicPoliciesByFactionId[selected.FactionId] = updated;
            RebuildRows();
            SetStatus($"Updated policy for {selected.FactionName}: tax {taxRatePercent:N1}% | incentive {incentivePercent:N1}%.", isError: false);
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
        AssignProtectionButton.IsEnabled = !isBusy;
        RecalculateButton.IsEnabled = !isBusy;
        ApplyEconomicPolicyButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
