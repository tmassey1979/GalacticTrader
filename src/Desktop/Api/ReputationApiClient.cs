using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class ReputationApiClient
{
    private readonly HttpClient _httpClient;

    public ReputationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<PlayerFactionStandingApiDto>> GetFactionStandingsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/reputation/factions/{playerId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load faction standings failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<PlayerFactionStandingApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<FactionBenefitApiDto>> GetFactionBenefitsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/reputation/factions/{playerId}/benefits", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load faction benefits failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<FactionBenefitApiDto>>(cancellationToken);
        return payload ?? [];
    }
}
