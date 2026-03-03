using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class FleetApiClient
{
    private readonly HttpClient _httpClient;

    public FleetApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<ShipApiDto>> GetPlayerShipsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/fleet/players/{playerId}/ships", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load ships failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<ShipApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<EscortSummaryApiDto?> GetEscortSummaryAsync(
        Guid playerId,
        string formation = "Defensive",
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/fleet/players/{playerId}/escort?formation={formation}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load escort summary failed ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<EscortSummaryApiDto>(cancellationToken);
    }
}
