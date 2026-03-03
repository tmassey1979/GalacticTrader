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
    private readonly ObservableCollection<ServicesStrategyBiasDistributionEntry> _biasDistribution = [];
    private readonly ObservableCollection<string> _contractLog = [];
    private readonly HashSet<Guid> _blacklistedAgentIds = [];
    private readonly Dictionary<Guid, NpcAgentApiDto> _agentsById = [];
    private bool _hasLoaded;

    public ServicesPanel(NpcApiClient npcApiClient)
    {
        _npcApiClient = npcApiClient;
        InitializeComponent();
        AgentsGrid.ItemsSource = _rows;
        BiasDistributionItems.ItemsSource = _biasDistribution;
        ContractLogList.ItemsSource = _contractLog;
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

            AppendContractLog("offer-contract", selected.Name, $"goal {result.CurrentGoal} tick {result.DecisionTick}");
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

            AppendContractLog("protection", selected.Name, $"fleet {summary.FleetSize} active {summary.ActiveShips}");
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
        AppendContractLog("blacklist", selected.Name, "agent blacklisted");
        SetStatus($"Blacklisted {selected.Name}.", isError: false);
    }

    private void OnClearBlacklistClick(object sender, RoutedEventArgs e)
    {
        _blacklistedAgentIds.Clear();
        RebuildRows();
        AppendContractLog("blacklist-clear", "system", "all entries removed");
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
            $"Standing {selected.PublicStanding} | WealthModel {selected.WealthModel}\n" +
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

        RebuildBiasDistribution();

        if (_rows.Count > 0)
        {
            AgentsGrid.SelectedIndex = 0;
        }
    }

    private void RebuildBiasDistribution()
    {
        var distribution = ServicesStrategyBiasDistributionBuilder.Build(_rows.ToArray());
        _biasDistribution.Clear();
        foreach (var entry in distribution)
        {
            _biasDistribution.Add(entry);
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
            WealthModel = string.Empty,
            PublicStanding = string.Empty,
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

    private void AppendContractLog(string action, string agentName, string detail)
    {
        var entry = ServicesContractLogFormatter.Build(DateTime.UtcNow, action, agentName, detail);
        _contractLog.Insert(0, entry);
        while (_contractLog.Count > 40)
        {
            _contractLog.RemoveAt(_contractLog.Count - 1);
        }
    }
}
