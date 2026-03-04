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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<GlobalMetricsSummaryApiDto> GetGlobalSummaryAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/api/telemetry/global-summary", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load global summary failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<GlobalMetricsSummaryApiDto>(response, cancellationToken);
        return payload ?? new GlobalMetricsSummaryApiDto();
    }
}



