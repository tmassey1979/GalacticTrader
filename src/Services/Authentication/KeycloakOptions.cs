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
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak realm name (e.g., "galactic-trader")
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2/OIDC client secret (for server-to-server auth)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Expected JWT audience claim
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to require HTTPS in production
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// JWT token validation parameters
    /// </summary>
    public JwtValidationOptions JwtValidation { get; set; } = new();
}

/// <summary>
/// JWT validation configuration
/// </summary>
public class JwtValidationOptions
{
    /// <summary>
    /// Validate token signature
    /// </summary>
    public bool ValidateSignature { get; set; } = true;

    /// <summary>
    /// Validate token expiration
    /// </summary>
    public bool ValidateExpiration { get; set; } = true;

    /// <summary>
    /// Validate issuer claim
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate audience claim
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance (seconds)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 5;

    /// <summary>
    /// Validate issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
