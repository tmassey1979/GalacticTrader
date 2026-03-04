using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GalacticTrader.Desktop.Api;

internal static class ApiClientRuntime
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static void SetBearerToken(HttpClient httpClient, string accessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ApiClientException(operation, response.StatusCode, detail);
    }

    public static Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
    }
}
