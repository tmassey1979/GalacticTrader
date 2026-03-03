using GalacticTrader.Desktop.Api;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GalacticTrader.Desktop.Routes;

public partial class RoutePlanningPanel : UserControl
{
    private readonly NavigationApiClient _navigationApiClient;
    private readonly ObservableCollection<RouteHopDisplayRow> _hops = [];
    private readonly ObservableCollection<RouteOptimizationDisplayRow> _optimizations = [];
    private IReadOnlyList<RouteSectorSelectionItem> _sectorItems = [];
    private bool _hasLoaded;

    public RoutePlanningPanel(NavigationApiClient navigationApiClient)
    {
        _navigationApiClient = navigationApiClient;

        InitializeComponent();
        HopsGrid.ItemsSource = _hops;
        OptimizationGrid.ItemsSource = _optimizations;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await LoadSectorsAsync();
    }

    private async Task LoadSectorsAsync()
    {
        SetBusy(true);
        try
        {
            var sectors = await _navigationApiClient.GetSectorsAsync();
            var items = sectors
                .OrderBy(static sector => sector.Name)
                .Select(static sector => new RouteSectorSelectionItem
                {
                    SectorId = sector.Id,
                    Name = sector.Name
                })
                .ToArray();
            _sectorItems = items;

            FromSectorCombo.ItemsSource = items;
            ToSectorCombo.ItemsSource = items;

            if (items.Length > 0)
            {
                FromSectorCombo.SelectedIndex = 0;
                ToSectorCombo.SelectedIndex = Math.Min(1, items.Length - 1);
            }

            SetStatus($"Loaded {items.Length} sectors.", isError: false);
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

    private async void OnCalculateClick(object sender, RoutedEventArgs e)
    {
        if (!TryBuildRoutePoints(out var routePoints))
        {
            return;
        }

        var selectedModePreset = ReadComboText(TravelModeCombo, fallback: "Balanced Route");
        var mode = RouteModePresetMapper.ToApiMode(selectedModePreset);
        var algorithm = ReadComboText(AlgorithmCombo, fallback: "dijkstra");

        SetBusy(true);
        try
        {
            var segments = new List<RoutePlanApiDto>();
            for (var index = 0; index < routePoints.Count - 1; index++)
            {
                var plan = await _navigationApiClient.GetRoutePlanAsync(routePoints[index], routePoints[index + 1], mode, algorithm);
                if (plan is null)
                {
                    _hops.Clear();
                    PlanSummaryText.Text = "No route plan found for selected sectors.";
                    RiskSummaryText.Text = "No risk simulation available.";
                    return;
                }

                segments.Add(plan);
            }

            var mergedPlan = RoutePlanAssembler.Combine(segments);
            RenderPlan(mergedPlan);
            var waypointCount = routePoints.Count - 2;
            SetStatus(waypointCount > 0
                ? $"Route plan calculated with {waypointCount} waypoint(s)."
                : "Route plan calculated.",
                isError: false);
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

    private async void OnOptimizeClick(object sender, RoutedEventArgs e)
    {
        if (!TryBuildRoutePoints(out var routePoints))
        {
            return;
        }

        SetBusy(true);
        try
        {
            var segmentOptimizations = new List<RouteOptimizationApiDto>();
            for (var index = 0; index < routePoints.Count - 1; index++)
            {
                var optimization = await _navigationApiClient.GetRouteOptimizationAsync(routePoints[index], routePoints[index + 1]);
                segmentOptimizations.Add(optimization);
            }

            var mergedOptimization = segmentOptimizations.Count == 1
                ? segmentOptimizations[0]
                : RouteOptimizationAssembler.Combine(segmentOptimizations);

            _optimizations.Clear();
            AddOptimization("Fastest", mergedOptimization.Fastest);
            AddOptimization("Cheapest", mergedOptimization.Cheapest);
            AddOptimization("Safest", mergedOptimization.Safest);
            AddOptimization("Balanced", mergedOptimization.Balanced);
            var waypointCount = routePoints.Count - 2;
            SetStatus(waypointCount > 0
                ? $"Loaded {_optimizations.Count} optimization profiles across {waypointCount} waypoint(s)."
                : $"Loaded {_optimizations.Count} optimization profiles.",
                isError: false);
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

    private bool TryGetSelectedSectors(out Guid fromSectorId, out Guid toSectorId)
    {
        fromSectorId = Guid.Empty;
        toSectorId = Guid.Empty;

        if (FromSectorCombo.SelectedItem is not RouteSectorSelectionItem fromItem ||
            ToSectorCombo.SelectedItem is not RouteSectorSelectionItem toItem)
        {
            SetStatus("Choose origin and destination sectors.", isError: true);
            return false;
        }

        if (fromItem.SectorId == toItem.SectorId)
        {
            SetStatus("Origin and destination must be different sectors.", isError: true);
            return false;
        }

        fromSectorId = fromItem.SectorId;
        toSectorId = toItem.SectorId;
        return true;
    }

    private bool TryBuildRoutePoints(out IReadOnlyList<Guid> routePoints)
    {
        routePoints = [];
        if (!TryGetSelectedSectors(out var fromSectorId, out var toSectorId))
        {
            return false;
        }

        if (!RouteWaypointParser.TryParse(
                WaypointsText.Text,
                _sectorItems,
                fromSectorId,
                toSectorId,
                out var waypointSectorIds,
                out var error))
        {
            SetStatus(error ?? "Invalid waypoint list.", isError: true);
            return false;
        }

        var points = new List<Guid> { fromSectorId };
        points.AddRange(waypointSectorIds);
        points.Add(toSectorId);
        routePoints = points;
        return true;
    }

    private void RenderPlan(RoutePlanApiDto plan)
    {
        _hops.Clear();
        foreach (var hop in plan.Hops)
        {
            _hops.Add(new RouteHopDisplayRow
            {
                Segment = $"{hop.FromSectorName} -> {hop.ToSectorName}",
                TravelTimeSeconds = hop.BaseTravelTimeSeconds,
                FuelCost = hop.BaseFuelCost,
                RiskScore = hop.BaseRiskScore
            });
        }

        PlanSummaryText.Text =
            $"Cost {plan.TotalCost:N2} | Fuel {plan.TotalFuelCost:N2} | Time {plan.TotalTravelTimeSeconds}s | Risk {plan.TotalRiskScore:N1}";

        var simulation = RouteRiskSimulationBuilder.Build(plan);
        RiskSummaryText.Text =
            $"RiskBand {simulation.RiskBand} | Intercept {simulation.InterceptionProbability:P1} | " +
            $"Loss {simulation.ExpectedLossProxy:N2} | Revenue {simulation.ExpectedRevenueProxy:N2}";
    }

    private void AddOptimization(string profile, RoutePlanApiDto? plan)
    {
        if (plan is null)
        {
            return;
        }

        _optimizations.Add(new RouteOptimizationDisplayRow
        {
            Profile = profile,
            TravelTimeSeconds = plan.TotalTravelTimeSeconds,
            FuelCost = plan.TotalFuelCost,
            RiskScore = plan.TotalRiskScore,
            TotalCost = plan.TotalCost
        });
    }

    private static string ReadComboText(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is ComboBoxItem selected
            ? selected.Content?.ToString() ?? fallback
            : fallback;
    }

    private void SetBusy(bool isBusy)
    {
        CalculateButton.IsEnabled = !isBusy;
        OptimizeButton.IsEnabled = !isBusy;
        WaypointsText.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }
}
