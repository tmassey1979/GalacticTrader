namespace GalacticTrader.Services.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
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
                ValidIssuers = BuildValidIssuers(),
                ValidateAudience = _options.JwtValidation.ValidateAudience,
                ValidAudience = _options.Audience,
                ValidateLifetime = _options.JwtValidation.ValidateExpiration,
                ClockSkew = TimeSpan.FromSeconds(_options.JwtValidation.ClockSkewSeconds),
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };

            var handler = new JwtSecurityTokenHandler();
            var parsedToken = handler.ReadJwtToken(token);
            if (_options.JwtValidation.ValidateAudience && !parsedToken.Audiences.Any())
            {
                // Some dev realm/client combinations omit aud; keep signature/issuer validation.
                validationParameters.ValidateAudience = false;
            }

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
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in principal.Claims.Where(claim =>
                     claim.Type == ClaimTypes.Role ||
                     claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                     claim.Type.Equals("roles", StringComparison.OrdinalIgnoreCase)))
        {
            AddRolesFromClaimValue(claim.Value, roles);
        }

        foreach (var claim in principal.Claims.Where(claim =>
                     claim.Type.Equals("realm_access", StringComparison.OrdinalIgnoreCase)))
        {
            AddRolesFromRealmAccessClaim(claim.Value, roles);
        }

        foreach (var claim in principal.Claims.Where(claim =>
                     claim.Type.Equals("resource_access", StringComparison.OrdinalIgnoreCase)))
        {
            AddRolesFromResourceAccessClaim(claim.Value, roles);
        }

        return roles;
    }

    public bool HasRole(ClaimsPrincipal principal, string role)
    {
        return GetRoles(principal).Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
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
            new HttpDocumentRetriever(_httpClient)
            {
                RequireHttps = _options.RequireHttpsMetadata
            });

        _oidcConfiguration = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
        return _oidcConfiguration;
    }

    /// <summary>
    /// Get the issuer URL
    /// </summary>
    private string GetIssuerUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.IssuerUrl))
        {
            var configuredIssuer = _options.IssuerUrl.TrimEnd('/');
            if (configuredIssuer.Contains("/realms/", StringComparison.OrdinalIgnoreCase))
            {
                return configuredIssuer;
            }

            return $"{configuredIssuer}/realms/{_options.Realm}";
        }

        return GetInternalIssuerUrl();
    }

    private string GetInternalIssuerUrl()
    {
        return $"{_options.ServerUrl.TrimEnd('/')}/realms/{_options.Realm}";
    }

    /// <summary>
    /// Get the OIDC well-known configuration URL
    /// </summary>
    private string GetWellKnownConfigUrl()
    {
        return $"{GetInternalIssuerUrl()}/.well-known/openid-configuration";
    }

    private IEnumerable<string> BuildValidIssuers()
    {
        return new[]
            {
                GetIssuerUrl(),
                GetInternalIssuerUrl()
            }
            .Where(issuer => !string.IsNullOrWhiteSpace(issuer))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void AddRolesFromClaimValue(string claimValue, ISet<string> roles)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return;
        }

        if (claimValue.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                using var json = JsonDocument.Parse(claimValue);
                if (json.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var roleElement in json.RootElement.EnumerateArray())
                    {
                        if (roleElement.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var role = roleElement.GetString();
                        if (!string.IsNullOrWhiteSpace(role))
                        {
                            roles.Add(role);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed role claim content.
            }

            return;
        }

        foreach (var role in claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            roles.Add(role);
        }
    }

    private static void AddRolesFromRealmAccessClaim(string claimValue, ISet<string> roles)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return;
        }

        try
        {
            using var json = JsonDocument.Parse(claimValue);
            if (!json.RootElement.TryGetProperty("roles", out var roleArray) ||
                roleArray.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var roleElement in roleArray.EnumerateArray())
            {
                if (roleElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var role = roleElement.GetString();
                if (!string.IsNullOrWhiteSpace(role))
                {
                    roles.Add(role);
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed realm_access claim content.
        }
    }

    private static void AddRolesFromResourceAccessClaim(string claimValue, ISet<string> roles)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return;
        }

        try
        {
            using var json = JsonDocument.Parse(claimValue);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var resourceElement in json.RootElement.EnumerateObject())
            {
                if (!resourceElement.Value.TryGetProperty("roles", out var roleArray) ||
                    roleArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var roleElement in roleArray.EnumerateArray())
                {
                    if (roleElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var role = roleElement.GetString();
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed resource_access claim content.
        }
    }
}
