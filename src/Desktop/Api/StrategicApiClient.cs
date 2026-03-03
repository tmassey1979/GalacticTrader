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

    public async Task<TerritoryDominanceApiDto?> RecalculateTerritoryDominanceAsync(
        Guid factionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/strategic/territory-dominance/recalculate/{factionId}", content: null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Recalculate territory dominance failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<TerritoryDominanceApiDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<TerritoryEconomicPolicyApiDto>> GetTerritoryEconomicPoliciesAsync(
        Guid? factionId = null,
        CancellationToken cancellationToken = default)
    {
        var path = factionId.HasValue
            ? $"/api/strategic/territory-economic-policy?factionId={factionId.Value:D}"
            : "/api/strategic/territory-economic-policy";

        var response = await _httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load territory economic policy failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<TerritoryEconomicPolicyApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<TerritoryEconomicPolicyApiDto?> UpsertTerritoryEconomicPolicyAsync(
        UpsertTerritoryEconomicPolicyApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/strategic/territory-economic-policy", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Update territory economic policy failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<TerritoryEconomicPolicyApiDto>(cancellationToken);
    }
}
