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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<NpcAgentApiDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/npc/agents", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load NPC agents failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<NpcAgentApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<NpcDecisionResultApiDto?> TickAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/npc/agents/{agentId}/tick", content: null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Tick NPC agent failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<NpcDecisionResultApiDto>(response, cancellationToken);
    }

    public async Task<NpcFleetSummaryApiDto?> SpawnFleetAsync(Guid agentId, int ships = 4, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"/api/npc/agents/{agentId}/fleet/spawn?ships={ships}", content: null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Spawn NPC fleet failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<NpcFleetSummaryApiDto>(response, cancellationToken);
    }
}



