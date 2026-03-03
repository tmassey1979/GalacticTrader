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
}
