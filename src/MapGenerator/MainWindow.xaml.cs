using GalacticTrader.MapGenerator.Api;
using GalacticTrader.MapGenerator.Generation;
using System.Globalization;
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
        SeedText.Text = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        SectorCountText.Text = "1200";
        RouteDensityText.Text = "2";
    }

    private void OnGenerateClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var seed = ParseSeed(SeedText.Text);
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
            MapGeneratorAuthHeaderConfigurator.ApplyBearerToken(httpClient, ApiTokenText.Text);
            var apiClient = new MapNavigationApiClient(httpClient);

            if (ReplaceExistingCheck.IsChecked == true)
            {
                var replacementResult = await ReplaceExistingMapAsync(apiClient);
                if (replacementResult.SectorDeleteWarnings.Count > 0)
                {
                    SetStatus(
                        $"Replace-existing completed with warnings. Routes deleted: {replacementResult.RoutesDeleted}, sectors deleted: {replacementResult.SectorsDeleted}, sectors kept: {replacementResult.SectorDeleteWarnings.Count}. Publishing new map anyway.",
                        isError: false);
                }
            }

            var createWarnings = new List<string>();
            var sectorIdByIndex = await CreateSectorsAsync(apiClient, _generatedLayout, createWarnings);
            await CreateRoutesAsync(apiClient, _generatedLayout, sectorIdByIndex);

            var publishMessage = $"Published {_generatedLayout.Sectors.Count} sectors and {_generatedLayout.Routes.Count} routes to {ApiBaseUrlText.Text.Trim()}.";
            if (createWarnings.Count > 0)
            {
                publishMessage += $" {createWarnings.Count} sector name conflict(s) were auto-resolved.";
            }

            SetStatus(publishMessage, isError: false);
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

    private async void OnLoadCurrentClick(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrlText.Text.Trim())
            };
            MapGeneratorAuthHeaderConfigurator.ApplyBearerToken(httpClient, ApiTokenText.Text);
            var apiClient = new MapNavigationApiClient(httpClient);

            var sectorsTask = apiClient.GetSectorsAsync();
            var routesTask = apiClient.GetRoutesAsync();
            await Task.WhenAll(sectorsTask, routesTask);

            var sectors = sectorsTask.Result;
            var routes = routesTask.Result;
            SectorList.ItemsSource = MapSnapshotProjector.BuildSectorRows(sectors);
            RouteList.ItemsSource = MapSnapshotProjector.BuildRouteRows(routes);
            SummaryText.Text = $"Loaded {sectors.Count} sectors and {routes.Count} routes from API.";
            SetStatus("Current map loaded from database snapshot.", isError: false);
        }
        catch (Exception exception)
        {
            SetStatus($"Load current map failed: {exception.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnFetchTokenClick(object sender, RoutedEventArgs e)
    {
        var loginWindow = new MapGeneratorLoginWindow(ApiBaseUrlText.Text.Trim())
        {
            Owner = this
        };

        var result = loginWindow.ShowDialog();
        if (result != true || string.IsNullOrWhiteSpace(loginWindow.AccessToken))
        {
            return;
        }

        ApiTokenText.Text = loginWindow.AccessToken;
        SetStatus("Bearer token acquired. You can now load and publish map data.", isError: false);
    }

    private static async Task<Dictionary<int, Guid>> CreateSectorsAsync(
        MapNavigationApiClient apiClient,
        GeneratedMapLayout layout,
        ICollection<string> warnings)
    {
        var sectorIdByIndex = new Dictionary<int, Guid>();
        foreach (var sector in layout.Sectors)
        {
            var sectorName = sector.Name;
            SectorApiDto? created = null;
            for (var attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    created = await apiClient.CreateSectorAsync(new CreateSectorApiRequest
                    {
                        Name = sectorName,
                        X = sector.X,
                        Y = sector.Y,
                        Z = sector.Z
                    });
                    break;
                }
                catch (InvalidOperationException exception) when (
                    IsConflict(exception) &&
                    attempt < 3)
                {
                    sectorName = $"{sector.Name}-{sector.Index + 1}-{attempt + 1}";
                    warnings.Add($"Sector '{sector.Name}' already existed. Created '{sectorName}' instead.");
                }
            }

            if (created is null)
            {
                throw new InvalidOperationException($"Failed to create sector '{sector.Name}' after conflict retries.");
            }

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

    private static async Task<(int RoutesDeleted, int SectorsDeleted, List<string> SectorDeleteWarnings)> ReplaceExistingMapAsync(MapNavigationApiClient apiClient)
    {
        var routes = await apiClient.GetRoutesAsync();
        var routesDeleted = 0;
        foreach (var route in routes)
        {
            await apiClient.DeleteRouteAsync(route.Id);
            routesDeleted++;
        }

        var sectors = await apiClient.GetSectorsAsync();
        var sectorsDeleted = 0;
        var warnings = new List<string>();
        foreach (var sector in sectors)
        {
            try
            {
                await apiClient.DeleteSectorAsync(sector.Id);
                sectorsDeleted++;
            }
            catch (InvalidOperationException exception)
            {
                warnings.Add($"{sector.Name}: {exception.Message}");
            }
        }

        return (routesDeleted, sectorsDeleted, warnings);
    }

    private void SetBusy(bool isBusy)
    {
        GenerateButton.IsEnabled = !isBusy;
        PublishButton.IsEnabled = !isBusy;
        LoadCurrentButton.IsEnabled = !isBusy;
        FetchTokenButton.IsEnabled = !isBusy;
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

    private static int ParseSeed(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Invalid seed. Enter a number or short text.");
        }

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intSeed))
        {
            return intSeed;
        }

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longSeed))
        {
            return FoldLongToInt(longSeed);
        }

        if (ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsignedSeed))
        {
            return FoldLongToInt(unchecked((long)unsignedSeed));
        }

        return HashSeedText(trimmed);
    }

    private static int FoldLongToInt(long value)
    {
        unchecked
        {
            return (int)(value ^ (value >> 32));
        }
    }

    private static int HashSeedText(string text)
    {
        unchecked
        {
            var hash = 2166136261;
            foreach (var character in text)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return (int)hash;
        }
    }

    private static bool IsConflict(InvalidOperationException exception)
    {
        return exception.Message.Contains("409", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase);
    }
}
