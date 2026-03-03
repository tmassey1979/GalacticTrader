using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class MarketApiClient
{
    private readonly HttpClient _httpClient;

    public MarketApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<TradeExecutionResultApiDto>> GetTransactionsAsync(
        Guid playerId,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/market/transactions/{playerId}?limit={limit}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load transactions failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<TradeExecutionResultApiDto>>(cancellationToken);
        return payload ?? [];
    }
}
