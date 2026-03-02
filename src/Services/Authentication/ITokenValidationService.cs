namespace GalacticTrader.Services.Authentication;

using System.Security.Claims;

/// <summary>
/// Service for validating and parsing JWT tokens from Keycloak
/// </summary>
public interface ITokenValidationService
{
    /// <summary>
    /// Validate a JWT token and return the claims principal
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Get the user ID from a claims principal
    /// </summary>
    Guid? GetUserId(ClaimsPrincipal principal);

    /// <summary>
    /// Get user's roles from claims principal
    /// </summary>
    IEnumerable<string> GetRoles(ClaimsPrincipal principal);

    /// <summary>
    /// Check if user has a specific role
    /// </summary>
    bool HasRole(ClaimsPrincipal principal, string role);

    /// <summary>
    /// Get all claims from token
    /// </summary>
    Dictionary<string, string> GetAllClaims(ClaimsPrincipal principal);
}
