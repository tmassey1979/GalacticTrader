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
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<MarketIntelligenceSummaryApiDto> GetSummaryAsync(int limit = 8, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/telemetry/market-intelligence?limit={Math.Clamp(limit, 3, 20)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load market intelligence failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<MarketIntelligenceSummaryApiDto>(cancellationToken);
        return payload ?? new MarketIntelligenceSummaryApiDto();
    }
}
