namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Service for authorization checks.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Get authorization context for user.
    /// </summary>
    Task<AuthorizationContext?> GetAuthorizationContextAsync(Guid userId);

    /// <summary>
    /// Create authorization context from claims.
    /// </summary>
    Task<AuthorizationContext?> CreateContextFromClaimsAsync(System.Security.Claims.ClaimsPrincipal principal);

    /// <summary>
    /// Check if user can perform action on resource.
    /// </summary>
    Task<bool> CanAccessResourceAsync(Guid userId, string resourceType, string resourceId);

    /// <summary>
    /// Check if user has permission.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permission);

    /// <summary>
    /// Validate authorization context.
    /// </summary>
    bool ValidateContext(AuthorizationContext context);
}
