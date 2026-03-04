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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<IntelligenceReportApiDto>> GetIntelligenceReportsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/strategic/intelligence/reports/{playerId}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load intelligence reports failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<IntelligenceReportApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<TerritoryDominanceApiDto>> GetTerritoryDominanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/strategic/territory-dominance", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load territory dominance failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<TerritoryDominanceApiDto>>(response, cancellationToken);
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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Recalculate territory dominance failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<TerritoryDominanceApiDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<TerritoryEconomicPolicyApiDto>> GetTerritoryEconomicPoliciesAsync(
        Guid? factionId = null,
        CancellationToken cancellationToken = default)
    {
        var path = factionId.HasValue
            ? $"/api/strategic/territory-economic-policy?factionId={factionId.Value:D}"
            : "/api/strategic/territory-economic-policy";

        var response = await _httpClient.GetAsync(path, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load territory economic policy failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<TerritoryEconomicPolicyApiDto>>(response, cancellationToken);
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

        await ApiClientRuntime.EnsureSuccessAsync(response, "Update territory economic policy failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<TerritoryEconomicPolicyApiDto>(response, cancellationToken);
    }
}



