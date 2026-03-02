namespace GalacticTrader.Services.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Implementation of JWT token validation using Keycloak OIDC
/// </summary>
public class KeycloakTokenValidationService : ITokenValidationService
{
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakTokenValidationService> _logger;
    private readonly HttpClient _httpClient;
    private IConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;
    private OpenIdConnectConfiguration? _oidcConfiguration;

    public KeycloakTokenValidationService(
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakTokenValidationService> logger,
        HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var configuration = await GetOidcConfigurationAsync();
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = _options.JwtValidation.ValidateIssuerSigningKey,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateIssuer = _options.JwtValidation.ValidateIssuer,
                ValidIssuer = GetIssuerUrl(),
                ValidateAudience = _options.JwtValidation.ValidateAudience,
                ValidAudience = _options.Audience,
                ValidateLifetime = _options.JwtValidation.ValidateExpiration,
                ClockSkew = TimeSpan.FromSeconds(_options.JwtValidation.ClockSkewSeconds),
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            _logger.LogInformation("Token validated successfully for user: {Subject}", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return principal;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Invalid token signature");
            return null;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Invalid token issuer");
            return null;
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogWarning(ex, "Invalid token audience");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    public Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? principal.FindFirst("sub")
            ?? principal.FindFirst("user_id");

        if (claim != null && Guid.TryParse(claim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    public IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        return principal.FindAll("roles")
            .Select(c => c.Value)
            .Where(r => !string.IsNullOrEmpty(r));
    }

    public bool HasRole(ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role) || GetRoles(principal).Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    public Dictionary<string, string> GetAllClaims(ClaimsPrincipal principal)
    {
        return principal.Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => string.Join(",", g.Select(c => c.Value)));
    }

    /// <summary>
    /// Get the OpenID Connect configuration from Keycloak
    /// </summary>
    private async Task<OpenIdConnectConfiguration> GetOidcConfigurationAsync()
    {
        if (_oidcConfiguration != null)
        {
            return _oidcConfiguration;
        }

        _configurationManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
            GetWellKnownConfigUrl(),
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(_httpClient));

        _oidcConfiguration = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
        return _oidcConfiguration;
    }

    /// <summary>
    /// Get the issuer URL
    /// </summary>
    private string GetIssuerUrl()
    {
        return $"{_options.ServerUrl}/realms/{_options.Realm}";
    }

    /// <summary>
    /// Get the OIDC well-known configuration URL
    /// </summary>
    private string GetWellKnownConfigUrl()
    {
        return $"{GetIssuerUrl()}/.well-known/openid-configuration";
    }
}
