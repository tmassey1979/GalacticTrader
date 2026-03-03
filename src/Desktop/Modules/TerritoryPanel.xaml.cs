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
            $"Sectors {selected.ControlledSectorCount} | Priority {selected.ProtectionPriority}";
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
            _recordsByFactionId.Clear();
            foreach (var row in dominance)
            {
                _recordsByFactionId[row.FactionId] = row;
            }

            RebuildRows();
            SetStatus($"Loaded {_rows.Count} territory records.", isError: false);
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
        var projected = TerritoryHeatmapProjector.Build(_recordsByFactionId.Values.ToArray(), _protectionPriorities);
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

    private void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;
        AssignProtectionButton.IsEnabled = !isBusy;
        RecalculateButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
