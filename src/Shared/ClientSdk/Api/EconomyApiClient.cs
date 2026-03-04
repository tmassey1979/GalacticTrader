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
        ApiClientRuntime.SetBearerToken(_httpClient, accessToken);
    }

    public async Task<PricePreviewApiResultDto> PreviewPriceAsync(
        PricePreviewApiRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/economy/price-preview", request, cancellationToken);
        await ApiClientRuntime.EnsureSuccessAsync(response, "Price preview failed", cancellationToken);

        var result = await ApiClientRuntime.ReadAsync<PricePreviewApiResultDto>(response, cancellationToken);
        return result ?? throw new InvalidOperationException("Price preview response was empty.");
    }
}



