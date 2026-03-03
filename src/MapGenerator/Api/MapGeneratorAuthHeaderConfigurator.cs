using System.Net.Http.Headers;
using System.Net.Http;

namespace GalacticTrader.MapGenerator.Api;

public static class MapGeneratorAuthHeaderConfigurator
{
    public static void ApplyBearerToken(HttpClient httpClient, string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        var token = rawToken.Trim();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
