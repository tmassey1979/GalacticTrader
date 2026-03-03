using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class NpcApiClient
{
    private readonly HttpClient _httpClient;

    public NpcApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<NpcAgentApiDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/npc/agents", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load NPC agents failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<NpcAgentApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<NpcDecisionResultApiDto?> TickAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/npc/agents/{agentId}/tick", content: null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Tick NPC agent failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<NpcDecisionResultApiDto>(cancellationToken);
    }

    public async Task<NpcFleetSummaryApiDto?> SpawnFleetAsync(Guid agentId, int ships = 4, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/npc/agents/{agentId}/fleet/spawn?ships={ships}", content: null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Spawn NPC fleet failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<NpcFleetSummaryApiDto>(cancellationToken);
    }
}
