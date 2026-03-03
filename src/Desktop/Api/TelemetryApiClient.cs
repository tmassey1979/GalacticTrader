using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class TelemetryApiClient
{
    private readonly HttpClient _httpClient;

    public TelemetryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<GlobalMetricsSummaryApiDto> GetGlobalSummaryAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/telemetry/global-summary", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load global summary failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<GlobalMetricsSummaryApiDto>(cancellationToken);
        return payload ?? new GlobalMetricsSummaryApiDto();
    }
}
