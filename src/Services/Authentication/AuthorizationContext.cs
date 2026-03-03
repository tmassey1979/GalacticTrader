namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Authorization context containing user info and claims.
/// </summary>
public class AuthorizationContext
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public Dictionary<string, string> CustomClaims { get; set; } = new();
    public DateTime TokenIssuedAt { get; set; }
    public DateTime TokenExpiresAt { get; set; }

    /// <summary>
    /// Check if user has any of the specified roles.
    /// </summary>
    public bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if user has all specified roles.
    /// </summary>
    public bool HasAllRoles(params string[] roles)
    {
        return roles.All(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if user is admin.
    /// </summary>
    public bool IsAdmin => HasAnyRole(AuthorizationPolicies.AdminRole);

    /// <summary>
    /// Check if user is moderator.
    /// </summary>
    public bool IsModerator => HasAnyRole(AuthorizationPolicies.ModeratorRole, AuthorizationPolicies.AdminRole);

    /// <summary>
    /// Check if token is still valid.
    /// </summary>
    public bool IsTokenValid => DateTime.UtcNow < TokenExpiresAt;
}
