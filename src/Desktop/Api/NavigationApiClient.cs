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
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<SectorApiDto>> GetSectorsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/navigation/sectors", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to load sectors ({(int)response.StatusCode}): {detail}");
        }

        var sectors = await response.Content.ReadFromJsonAsync<List<SectorApiDto>>(cancellationToken);
        return sectors ?? [];
    }

    public async Task<IReadOnlyList<RouteApiDto>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/navigation/routes", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to load routes ({(int)response.StatusCode}): {detail}");
        }

        var routes = await response.Content.ReadFromJsonAsync<List<RouteApiDto>>(cancellationToken);
        return routes ?? [];
    }

    public async Task<IReadOnlyList<RouteApiDto>> GetDangerousRoutesAsync(
        int riskThreshold = 70,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/navigation/routes/dangerous?riskThreshold={riskThreshold}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to load dangerous routes ({(int)response.StatusCode}): {detail}");
        }

        var routes = await response.Content.ReadFromJsonAsync<List<RouteApiDto>>(cancellationToken);
        return routes ?? [];
    }

    public async Task<SectorApiDto> CreateSectorAsync(CreateSectorApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/navigation/sectors", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Create sector failed ({(int)response.StatusCode}): {detail}");
        }

        var sector = await response.Content.ReadFromJsonAsync<SectorApiDto>(cancellationToken);
        return sector ?? throw new InvalidOperationException("Create sector response was empty.");
    }

    public async Task<RouteApiDto> CreateRouteAsync(CreateRouteApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/navigation/routes", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Create route failed ({(int)response.StatusCode}): {detail}");
        }

        var route = await response.Content.ReadFromJsonAsync<RouteApiDto>(cancellationToken);
        return route ?? throw new InvalidOperationException("Create route response was empty.");
    }
}
