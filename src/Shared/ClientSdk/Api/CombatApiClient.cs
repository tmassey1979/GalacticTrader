using System.Net.Http;
using System.Net.Http.Json;
using System.Net;

namespace GalacticTrader.Desktop.Api;

public sealed class CombatApiClient
{
    private readonly HttpClient _httpClient;

    public CombatApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<CombatLogApiDto>> GetRecentLogsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/combat/logs?limit={limit}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load combat logs failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<CombatLogApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<CombatSummaryApiDto>> GetActiveCombatsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/combat/active", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load active combats failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<CombatSummaryApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<CombatSummaryApiDto?> GetCombatAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/combat/{combatId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Load combat failed", cancellationToken);
        return await ApiClientRuntime.ReadAsync<CombatSummaryApiDto>(response, cancellationToken);
    }

    public async Task<CombatSummaryApiDto> StartCombatAsync(StartCombatApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/combat/start", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Start combat failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<CombatSummaryApiDto>(response, cancellationToken);
        return payload ?? throw new InvalidOperationException("Start combat response was empty.");
    }

    public async Task<CombatTickResultApiDto?> ProcessTickAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/combat/{combatId}/tick", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Combat tick failed", cancellationToken);
        return await ApiClientRuntime.ReadAsync<CombatTickResultApiDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<CombatTickResultApiDto>> ProcessTicksAsync(
        Guid combatId,
        int tickCount,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/combat/{combatId}/ticks?count={Math.Max(1, tickCount)}", content: null, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Combat ticks failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<CombatTickResultApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<CombatSummaryApiDto?> EndCombatAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/combat/{combatId}/end", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "End combat failed", cancellationToken);
        return await ApiClientRuntime.ReadAsync<CombatSummaryApiDto>(response, cancellationToken);
    }
}



