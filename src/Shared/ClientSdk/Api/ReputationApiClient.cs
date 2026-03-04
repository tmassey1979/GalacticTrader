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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<IReadOnlyList<PlayerFactionStandingApiDto>> GetFactionStandingsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/reputation/factions/{playerId}", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load faction standings failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<PlayerFactionStandingApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<IReadOnlyList<FactionBenefitApiDto>> GetFactionBenefitsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/reputation/factions/{playerId}/benefits", cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Load faction benefits failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<List<FactionBenefitApiDto>>(response, cancellationToken);
        return payload ?? [];
    }

    public async Task<AlignmentAccessApiDto?> GetAlignmentAccessAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/reputation/alignment/{playerId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Load alignment access failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<AlignmentAccessApiDto>(response, cancellationToken);
    }

    public async Task<AlignmentStateApiDto?> ApplyAlignmentActionAsync(
        AlignmentActionApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/reputation/alignment/action", request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await ApiClientRuntime.EnsureSuccessAsync(response, "Apply alignment action failed", cancellationToken);

        return await ApiClientRuntime.ReadAsync<AlignmentStateApiDto>(response, cancellationToken);
    }
}



