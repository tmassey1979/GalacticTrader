using GalacticTrader.MapGenerator.Api;
using GalacticTrader.MapGenerator.Generation;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GalacticTrader.MapGenerator;

public partial class MainWindow : Window
{
    private readonly MapLayoutGenerator _layoutGenerator = new();
    private GeneratedMapLayout? _generatedLayout;

    public MainWindow()
    {
        InitializeComponent();

        var options = MapGeneratorApiOptions.FromEnvironment();
        ApiBaseUrlText.Text = options.BaseUrl;
        SeedText.Text = DateTime.UtcNow.Ticks.ToString();
        SectorCountText.Text = "32";
        RouteDensityText.Text = "2";
    }

    private void OnGenerateClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var seed = ParseInteger(SeedText.Text, "seed");
            var sectorCount = ParseInteger(SectorCountText.Text, "sector count");
            var routeDensity = ParseInteger(RouteDensityText.Text, "route density");

            _generatedLayout = _layoutGenerator.Generate(seed, sectorCount, routeDensity);
            SectorList.ItemsSource = _generatedLayout.Sectors
                .Select(sector => $"{sector.Name} [{sector.X:F1}, {sector.Y:F1}, {sector.Z:F1}]")
                .ToList();
            RouteList.ItemsSource = _generatedLayout.Routes
                .Select(route => $"{route.FromIndex} -> {route.ToIndex} {(route.IsHighRisk ? "(High Risk)" : "(Standard)")}")
                .ToList();

            SummaryText.Text = $"Generated {_generatedLayout.Sectors.Count} sectors and {_generatedLayout.Routes.Count} routes.";
            SetStatus("Preview generated. Publish to persist in the API database.", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
    }

    private async void OnPublishClick(object sender, RoutedEventArgs e)
    {
        if (_generatedLayout is null)
        {
            SetStatus("Generate a map preview before publishing.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrlText.Text.Trim())
            };
            var apiClient = new MapNavigationApiClient(httpClient);

            if (ReplaceExistingCheck.IsChecked == true)
            {
                await ReplaceExistingMapAsync(apiClient);
            }

            var sectorIdByIndex = await CreateSectorsAsync(apiClient, _generatedLayout);
            await CreateRoutesAsync(apiClient, _generatedLayout, sectorIdByIndex);

            SetStatus(
                $"Published {_generatedLayout.Sectors.Count} sectors and {_generatedLayout.Routes.Count} routes to {ApiBaseUrlText.Text.Trim()}.",
                isError: false);
        }
        catch (Exception exception)
        {
            SetStatus($"Publish failed: {exception.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static async Task<Dictionary<int, Guid>> CreateSectorsAsync(MapNavigationApiClient apiClient, GeneratedMapLayout layout)
    {
        var sectorIdByIndex = new Dictionary<int, Guid>();
        foreach (var sector in layout.Sectors)
        {
            var created = await apiClient.CreateSectorAsync(new CreateSectorApiRequest
            {
                Name = sector.Name,
                X = sector.X,
                Y = sector.Y,
                Z = sector.Z
            });

            sectorIdByIndex[sector.Index] = created.Id;
        }

        return sectorIdByIndex;
    }

    private static async Task CreateRoutesAsync(
        MapNavigationApiClient apiClient,
        GeneratedMapLayout layout,
        Dictionary<int, Guid> sectorIdByIndex)
    {
        foreach (var route in layout.Routes)
        {
            if (!sectorIdByIndex.TryGetValue(route.FromIndex, out var fromSectorId))
            {
                continue;
            }

            if (!sectorIdByIndex.TryGetValue(route.ToIndex, out var toSectorId))
            {
                continue;
            }

            await apiClient.CreateRouteAsync(new CreateRouteApiRequest
            {
                FromSectorId = fromSectorId,
                ToSectorId = toSectorId,
                LegalStatus = route.IsHighRisk ? "Illegal" : "Legal",
                WarpGateType = route.IsHighRisk ? "Unstable" : "Stable"
            });
        }
    }

    private static async Task ReplaceExistingMapAsync(MapNavigationApiClient apiClient)
    {
        var routes = await apiClient.GetRoutesAsync();
        foreach (var route in routes)
        {
            await apiClient.DeleteRouteAsync(route.Id);
        }

        var sectors = await apiClient.GetSectorsAsync();
        foreach (var sector in sectors)
        {
            await apiClient.DeleteSectorAsync(sector.Id);
        }
    }

    private void SetBusy(bool isBusy)
    {
        GenerateButton.IsEnabled = !isBusy;
        PublishButton.IsEnabled = !isBusy;
        Mouse.OverrideCursor = isBusy ? System.Windows.Input.Cursors.Wait : null;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(125, 241, 166));
    }

    private static int ParseInteger(string value, string label)
    {
        if (!int.TryParse(value.Trim(), out var parsed))
        {
            throw new InvalidOperationException($"Invalid {label}. Enter a valid integer.");
        }

        return parsed;
    }
}
