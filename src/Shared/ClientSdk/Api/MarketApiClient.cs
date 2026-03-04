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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<TradeExecutionResultApiDto>> GetTransactionsAsync(
        Guid playerId,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/market/transactions/{playerId}?limit={limit}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load transactions failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<TradeExecutionResultApiDto>>(response, cancellationToken);
        return payload ?? [];
    }
}



