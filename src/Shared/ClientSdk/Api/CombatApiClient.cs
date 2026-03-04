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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<CombatLogApiDto>> GetRecentLogsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/combat/logs?limit={limit}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load combat logs failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<CombatLogApiDto>>(response, cancellationToken);
        return payload ?? [];
    }
}



