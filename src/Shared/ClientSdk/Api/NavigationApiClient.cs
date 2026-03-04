using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class NavigationApiClient
{
    private readonly HttpClient _httpClient;

    public NavigationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<SectorApiDto>> GetSectorsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/navigation/sectors", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Failed to load sectors", cancellationToken);

        var sectors = await ApiClientRuntime.ReadAsync<List<SectorApiDto>>(response, cancellationToken);
        return sectors ?? [];
    }

    public async Task<IReadOnlyList<RouteApiDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/navigation/routes", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Failed to load routes", cancellationToken);

        var routes = await ApiClientRuntime.ReadAsync<List<RouteApiDto>>(response, cancellationToken);
        return routes ?? [];
    }

    public async Task<IReadOnlyList<RouteApiDto>> GetDangerousRoutesAsync(
        int riskThreshold = 70,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/navigation/routes/dangerous?riskThreshold={riskThreshold}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Failed to load dangerous routes", cancellationToken);

        var routes = await ApiClientRuntime.ReadAsync<List<RouteApiDto>>(response, cancellationToken);
        return routes ?? [];
    }

    public async Task<RoutePlanApiDto?> GetRoutePlanAsync(
        Guid fromSectorId,
        Guid toSectorId,
        string travelMode = "Standard",
        string algorithm = "dijkstra",
        CancellationToken cancellationToken = default)
    {
        var escapedMode = Uri.EscapeDataString(travelMode);
        var escapedAlgorithm = Uri.EscapeDataString(algorithm);
        var url = $"/api/navigation/planning/{fromSectorId}/{toSectorId}?mode={escapedMode}&algorithm={escapedAlgorithm}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Route planning failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<RoutePlanApiDto>(response, cancellationToken);
    }

    public async Task<RouteOptimizationApiDto> GetRouteOptimizationAsync(
        Guid fromSectorId,
        Guid toSectorId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/navigation/planning/{fromSectorId}/{toSectorId}/optimize", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Route optimization failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<RouteOptimizationApiDto>(response, cancellationToken);
        return payload ?? new RouteOptimizationApiDto();
    }

    public async Task<SectorApiDto> CreateSectorAsync(CreateSectorApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/navigation/sectors", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Create sector failed", cancellationToken);

        var sector = await ApiClientRuntime.ReadAsync<SectorApiDto>(response, cancellationToken);
        return sector ?? throw new InvalidOperationException("Create sector response was empty.");
    }

    public async Task<RouteApiDto> CreateRouteAsync(CreateRouteApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/navigation/routes", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Create route failed", cancellationToken);

        var route = await ApiClientRuntime.ReadAsync<RouteApiDto>(response, cancellationToken);
        return route ?? throw new InvalidOperationException("Create route response was empty.");
    }
}



