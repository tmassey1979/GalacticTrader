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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<LeaderboardEntryApiDto>> GetLeaderboardAsync(
        string leaderboardType,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = string.IsNullOrWhiteSpace(leaderboardType) ? "reputation" : leaderboardType.Trim();
        var response = await _httpClient.GetAsync($"/api/leaderboards/{normalizedType}?limit={Math.Clamp(limit, 1, 200)}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load leaderboard failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<LeaderboardEntryApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<LeaderboardEntryApiDto>> RecalculateAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("/api/leaderboards/recalculate", content: null, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            return [];
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Recalculate leaderboard failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<LeaderboardEntryApiDto>>(response, cancellationToken);
        return payload ?? [];
    }
}



