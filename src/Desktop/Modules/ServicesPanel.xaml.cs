using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Modules;

public partial class ServicesPanel : UserControl
{
    private readonly NpcApiClient _npcApiClient;
    private readonly ObservableCollection<ServicesAgentDisplayRow> _rows = [];
    private readonly HashSet<Guid> _blacklistedAgentIds = [];
    private readonly Dictionary<Guid, NpcAgentApiDto> _agentsById = [];
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

    private async void OnOfferContractClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedAgent(out var selected))
        {
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _npcApiClient.TickAgentAsync(selected.AgentId);
            if (result is null)
            {
                SetStatus("Selected agent was not found.", isError: true);
                return;
            }

            SetStatus($"Contract offered. Goal changed to {result.CurrentGoal} (tick {result.DecisionTick}).", isError: false);
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

    private async void OnNegotiateProtectionClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedAgent(out var selected))
        {
            return;
        }

        SetBusy(true);
        try
        {
            var summary = await _npcApiClient.SpawnFleetAsync(selected.AgentId, ships: 4);
            if (summary is null)
            {
                SetStatus("Selected agent was not found.", isError: true);
                return;
            }

            SetStatus($"Protection negotiated. Fleet size {summary.FleetSize}, active ships {summary.ActiveShips}.", isError: false);
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

    private void OnBlacklistClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedAgent(out var selected))
        {
            return;
        }

        _blacklistedAgentIds.Add(selected.AgentId);
        RebuildRows();
        SetStatus($"Blacklisted {selected.Name}.", isError: false);
    }

    private void OnClearBlacklistClick(object sender, RoutedEventArgs e)
    {
        _blacklistedAgentIds.Clear();
        RebuildRows();
        SetStatus("Blacklist cleared.", isError: false);
    }

    private void OnAgentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!TryGetSelectedAgent(out var selected))
        {
            SelectedAgentText.Text = "Select an NPC agent.";
            return;
        }

        SelectedAgentText.Text =
            $"{selected.Name} [{selected.Archetype}] | Influence {selected.InfluenceScore:N2}\n" +
            $"Aggression {selected.AggressionIndex} | Wealth {selected.Wealth:N0} | Fleet {selected.FleetSize}\n" +
            $"Goal: {selected.CurrentGoal} | Bias: {selected.StrategyBias}";
    }

    private async Task RefreshAsync()
    {
        SetBusy(true);
        try
        {
            var agents = await _npcApiClient.GetAgentsAsync();
            _agentsById.Clear();
            foreach (var agent in agents)
            {
                _agentsById[agent.Id] = agent;
            }

            RebuildRows();
            SetStatus($"Loaded {_rows.Count} NPC agents.", isError: false);
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
        var projected = ServicesAgentProjector.Build(_agentsById.Values.ToArray(), _blacklistedAgentIds);
        _rows.Clear();
        foreach (var row in projected)
        {
            _rows.Add(row);
        }

        if (_rows.Count > 0)
        {
            AgentsGrid.SelectedIndex = 0;
        }
    }

    private bool TryGetSelectedAgent(out ServicesAgentDisplayRow selected)
    {
        if (AgentsGrid.SelectedItem is ServicesAgentDisplayRow row)
        {
            selected = row;
            return true;
        }

        selected = new ServicesAgentDisplayRow
        {
            AgentId = Guid.Empty,
            Name = string.Empty,
            Archetype = string.Empty,
            StrategyBias = string.Empty,
            CurrentGoal = string.Empty
        };

        SetStatus("Select an NPC agent first.", isError: true);
        return false;
    }

    private void SetBusy(bool isBusy)
    {
        RefreshButton.IsEnabled = !isBusy;
        OfferContractButton.IsEnabled = !isBusy;
        NegotiateProtectionButton.IsEnabled = !isBusy;
        BlacklistButton.IsEnabled = !isBusy;
        ClearBlacklistButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
