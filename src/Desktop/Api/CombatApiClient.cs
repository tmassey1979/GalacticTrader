using System.Net.Http;
using System.Net.Http.Json;

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
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<CombatLogApiDto>> GetRecentLogsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/combat/logs?limit={limit}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load combat logs failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<CombatLogApiDto>>(cancellationToken);
        return payload ?? [];
    }
}
