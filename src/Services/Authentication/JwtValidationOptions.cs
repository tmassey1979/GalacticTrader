namespace GalacticTrader.Services.Authentication;

/// <summary>
/// JWT validation configuration.
/// </summary>
public class JwtValidationOptions
{
    /// <summary>
    /// Validate token signature.
    /// </summary>
    public bool ValidateSignature { get; set; } = true;

    /// <summary>
    /// Validate token expiration.
    /// </summary>
    public bool ValidateExpiration { get; set; } = true;

    /// <summary>
    /// Validate issuer claim.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate audience claim.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance (seconds).
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 5;

    /// <summary>
    /// Validate issuer signing key.
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
