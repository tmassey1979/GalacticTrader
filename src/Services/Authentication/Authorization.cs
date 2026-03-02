namespace GalacticTrader.Services.Authentication;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Defines authorization policies and roles for game features
/// </summary>
public static class AuthorizationPolicies
{
    // Role definitions
    public const string AdminRole = "admin";
    public const string ModeratorRole = "moderator";
    public const string PlayerRole = "player";
    public const string BotRole = "bot";

    // Policy names
    public const string PlayerPolicy = "PlayerPolicy";
    public const string ModeratorPolicy = "ModeratorPolicy";
    public const string AdminPolicy = "AdminPolicy";

    /// <summary>
    /// All available game roles
    /// </summary>
    public static readonly string[] AllRoles = { AdminRole, ModeratorRole, PlayerRole, BotRole };

    /// <summary>
    /// Roles allowed to manage game
    /// </summary>
    public static readonly string[] ManagementRoles = { AdminRole, ModeratorRole };
}

/// <summary>
/// Attribute for requiring specific roles
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoleAttribute : Attribute
{
    public string[] Roles { get; }

    public AuthorizeRoleAttribute(params string[] roles)
    {
        Roles = roles ?? Array.Empty<string>();
    }
}

/// <summary>
/// Attribute for requiring specific permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizePermissionAttribute : Attribute
{
    public string Permission { get; }

    public AuthorizePermissionAttribute(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// Authorization context containing user info and claims
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
    /// Check if user has any of the specified roles
    /// </summary>
    public bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if user has all specified roles
    /// </summary>
    public bool HasAllRoles(params string[] roles)
    {
        return roles.All(role => Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if user is admin
    /// </summary>
    public bool IsAdmin => HasAnyRole(AuthorizationPolicies.AdminRole);

    /// <summary>
    /// Check if user is moderator
    /// </summary>
    public bool IsModerator => HasAnyRole(AuthorizationPolicies.ModeratorRole, AuthorizationPolicies.AdminRole);

    /// <summary>
    /// Check if token is still valid
    /// </summary>
    public bool IsTokenValid => DateTime.UtcNow < TokenExpiresAt;
}

/// <summary>
/// Service for authorization checks
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Get authorization context for user
    /// </summary>
    Task<AuthorizationContext?> GetAuthorizationContextAsync(Guid userId);

    /// <summary>
    /// Create authorization context from claims
    /// </summary>
    Task<AuthorizationContext?> CreateContextFromClaimsAsync(System.Security.Claims.ClaimsPrincipal principal);

    /// <summary>
    /// Check if user can perform action on resource
    /// </summary>
    Task<bool> CanAccessResourceAsync(Guid userId, string resourceType, string resourceId);

    /// <summary>
    /// Check if user has permission
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission);

    /// <summary>
    /// Validate authorization context
    /// </summary>
    bool ValidateContext(AuthorizationContext context);
}
