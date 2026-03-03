namespace GalacticTrader.MapGenerator.Api;

public sealed class MapGeneratorIdentityOptions
{
    public required string KeycloakBaseUrl { get; init; }
    public required string Realm { get; init; }
    public required string ClientId { get; init; }

    public static MapGeneratorIdentityOptions FromEnvironment(string apiBaseUrl)
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("GT_KEYCLOAK_BASE_URL");
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            configuredBaseUrl = InferKeycloakUrlFromApiBase(apiBaseUrl) ?? "http://localhost:8180";
        }

        var realm = Environment.GetEnvironmentVariable("GT_KEYCLOAK_REALM");
        if (string.IsNullOrWhiteSpace(realm))
        {
            realm = "galactictrader";
        }

        var clientId = Environment.GetEnvironmentVariable("GT_KEYCLOAK_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = "map-generator-desktop";
        }

        return new MapGeneratorIdentityOptions
        {
            KeycloakBaseUrl = configuredBaseUrl.TrimEnd('/'),
            Realm = realm.Trim(),
            ClientId = clientId.Trim()
        };
    }

    private static string? InferKeycloakUrlFromApiBase(string apiBaseUrl)
    {
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
        {
            return null;
        }

        var keycloakBuilder = new UriBuilder(apiBaseUri)
        {
            Port = 8180,
            Path = string.Empty,
            Query = string.Empty
        };

        return keycloakBuilder.Uri.ToString().TrimEnd('/');
    }
}
