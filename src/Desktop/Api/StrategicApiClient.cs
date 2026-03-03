using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class StrategicApiClient
{
    private readonly HttpClient _httpClient;

    public StrategicApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<IntelligenceReportApiDto>> GetIntelligenceReportsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/strategic/intelligence/reports/{playerId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load intelligence reports failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<IntelligenceReportApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<TerritoryDominanceApiDto>> GetTerritoryDominanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/strategic/territory-dominance", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load territory dominance failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<TerritoryDominanceApiDto>>(cancellationToken);
        return payload ?? [];
    }
}
