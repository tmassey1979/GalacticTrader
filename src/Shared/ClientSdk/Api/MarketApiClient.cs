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

    public async Task<IReadOnlyList<MarketListingApiDto>> GetListingsAsync(
        int limit = 80,
        Guid? marketId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 300);
        var query = marketId.HasValue
            ? $"/api/market/listings?limit={normalizedLimit}&marketId={marketId.Value}"
            : $"/api/market/listings?limit={normalizedLimit}";
        var response = await _httpClient.GetAsync(query, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load market listings failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<MarketListingApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<TradeExecutionResultApiDto> ExecuteTradeAsync(
        ExecuteTradeApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/market/trade", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Execute trade failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<TradeExecutionResultApiDto>(response, cancellationToken);
        return payload ?? throw new InvalidOperationException("Trade execution response was empty.");
    }

    public async Task<TradeExecutionResultApiDto?> ReverseTradeAsync(
        ReverseTradeApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/market/trade/reverse", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Reverse trade failed", cancellationToken);
        return await ApiClientRuntime.ReadAsync<TradeExecutionResultApiDto>(response, cancellationToken);
    }
}



