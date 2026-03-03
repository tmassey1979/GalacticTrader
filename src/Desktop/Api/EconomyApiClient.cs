using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class EconomyApiClient
{
    private readonly HttpClient _httpClient;

    public EconomyApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public void SetBearerToken(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<PricePreviewApiResultDto> PreviewPriceAsync(
        PricePreviewApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/economy/price-preview", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Price preview failed ({(int)response.StatusCode}): {detail}");
        }

        var result = await response.Content.ReadFromJsonAsync<PricePreviewApiResultDto>(cancellationToken);
        return result ?? throw new InvalidOperationException("Price preview response was empty.");
    }
}
