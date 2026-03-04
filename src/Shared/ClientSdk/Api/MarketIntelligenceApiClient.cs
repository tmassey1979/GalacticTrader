using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class MarketIntelligenceApiClient
{
    private readonly HttpClient _httpClient;

    public MarketIntelligenceApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<MarketIntelligenceSummaryApiDto> GetSummaryAsync(int limit = 8, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/telemetry/market-intelligence?limit={Math.Clamp(limit, 3, 20)}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load market intelligence failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<MarketIntelligenceSummaryApiDto>(response, cancellationToken);
        return payload ?? new MarketIntelligenceSummaryApiDto();
    }
}



