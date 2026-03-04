namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Configuration options for Keycloak authentication
/// </summary>
public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Keycloak server URL (e.g., http://localhost:8080)
    /// </summary>
    public string ServerUrl { get; set; } = "http://localhost:8180";

    /// <summary>
    /// Optional issuer base URL or full issuer URL used for validating incoming tokens.
    /// </summary>
    public string IssuerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak realm name (e.g., "galactic-trader")
    /// </summary>
    public string Realm { get; set; } = "galactictrader";

    /// <summary>
    /// OAuth2/OIDC client ID
    /// </summary>
    public string ClientId { get; set; } = "map-generator-desktop";

    /// <summary>
    /// OAuth2/OIDC client secret (for server-to-server auth)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Expected JWT audience claim
    /// </summary>
    public string Audience { get; set; } = "account";

    /// <summary>
    /// Whether to require HTTPS in production
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// When Keycloak rejects credentials, allow fallback to local auth store.
    /// Useful for hybrid/dev scenarios where local users coexist with federated auth.
    /// </summary>
    public bool AllowLocalFallbackOnInvalidCredentials { get; set; } = true;

    /// <summary>
    /// JWT token validation parameters
    /// </summary>
    public JwtValidationOptions JwtValidation { get; set; } = new();
}
