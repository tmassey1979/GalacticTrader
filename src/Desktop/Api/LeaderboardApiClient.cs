using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class LeaderboardApiClient
{
    private readonly HttpClient _httpClient;

    public LeaderboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<IReadOnlyList<LeaderboardEntryApiDto>> GetLeaderboardAsync(
        string leaderboardType,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = string.IsNullOrWhiteSpace(leaderboardType) ? "reputation" : leaderboardType.Trim();
        var response = await _httpClient.GetAsync($"/api/leaderboards/{normalizedType}?limit={Math.Clamp(limit, 1, 200)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Load leaderboard failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<LeaderboardEntryApiDto>>(cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<LeaderboardEntryApiDto>> RecalculateAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/api/leaderboards/recalculate", content: null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Recalculate leaderboard failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<List<LeaderboardEntryApiDto>>(cancellationToken);
        return payload ?? [];
    }
}
