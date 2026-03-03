using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Fleet;

public partial class FleetPanel : UserControl
{
    private readonly DesktopSession _session;
    private readonly FleetApiClient _fleetApiClient;
    private readonly ObservableCollection<FleetShipDisplayRow> _shipRows = [];
    private readonly Dictionary<Guid, ShipApiDto> _shipsById = [];
    private bool _hasLoaded;

    public FleetPanel(DesktopSession session, FleetApiClient fleetApiClient)
    {
        _session = session;
        _fleetApiClient = fleetApiClient;

        InitializeComponent();
        ShipsGrid.ItemsSource = _shipRows;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await RefreshFleetAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshFleetAsync();
    }

    private void OnShipSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShipsGrid.SelectedItem is not FleetShipDisplayRow row ||
            !_shipsById.TryGetValue(row.ShipId, out var ship))
        {
            SelectedShipText.Text = "Select a ship to inspect modules.";
            ModulesList.ItemsSource = null;
            return;
        }

        SelectedShipText.Text =
            $"{ship.Name} ({ship.ShipClass}) | Hull {ship.HullIntegrity}/{ship.MaxHullIntegrity} | " +
            $"Cargo {ship.CargoCapacity} | Reactor {ship.ReactorOutput} | " +
            $"{FleetInsuranceStatusFormatter.Format(ship.HasInsurance, ship.InsuranceRate)} | " +
            $"Route {ship.AssignedRoute}";

        var moduleLines = ship.Modules.Count == 0
            ? new[] { "No modules installed." }
            : ship.Modules
                .Select(module => $"{module.Name} [{module.ModuleType}] Tier {module.Tier}")
                .ToArray();
        ModulesList.ItemsSource = moduleLines;
    }

    private async Task RefreshFleetAsync()
    {
        SetBusy(true);
        try
        {
            var ships = await _fleetApiClient.GetPlayerShipsAsync(_session.PlayerId);
            var escort = await _fleetApiClient.GetEscortSummaryAsync(_session.PlayerId);

            _shipsById.Clear();
            _shipRows.Clear();

            foreach (var ship in ships)
            {
                _shipsById[ship.Id] = ship;
                var utilization = FleetCrewUtilizationProjector.Build(ship.CrewCount, ship.CrewSlots);
                _shipRows.Add(new FleetShipDisplayRow
                {
                    ShipId = ship.Id,
                    Name = ship.Name,
                    ShipClass = ship.ShipClass,
                    HullIntegrity = ship.HullIntegrity,
                    MaxHullIntegrity = ship.MaxHullIntegrity,
                    CargoCapacity = ship.CargoCapacity,
                    ModuleCount = ship.Modules.Count,
                    CrewAssignment = $"{utilization.CrewCount}/{ship.CrewSlots}",
                    CrewStatus = utilization.Status,
                    InsuranceStatus = FleetInsuranceStatusFormatter.Format(ship.HasInsurance, ship.InsuranceRate),
                    AssignedRoute = ship.AssignedRoute
                });
            }

            if (_shipRows.Count > 0 && ShipsGrid.SelectedItem is null)
            {
                ShipsGrid.SelectedIndex = 0;
            }

            EscortSummaryText.Text = escort is null
                ? "Escort summary unavailable."
                : $"FleetStrength: {escort.FleetStrength} | EscortStrength: {escort.EscortStrength}\n" +
                  $"Coordination: {escort.CoordinationBonus:P1} | Convoy Bonus: {escort.ConvoyBonus:P1}\n" +
                  $"Protective Range: {escort.ProtectiveRange:N1} | Combat Mod: {escort.CombatModifier:P1}";

            SetStatus($"Loaded {_shipRows.Count} ships for {_session.Username}.", isError: false);
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
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
