namespace GalacticTrader.Services.Authentication;

/// <summary>
/// Service for managing user accounts and user-Keycloak integration
/// </summary>
public interface IUserAccountService
{
    /// <summary>
    /// Create a new user account
    /// </summary>
    Task<GalacticTrader.Data.Models.UserAccount> CreateUserAsync(CreateUserRequest request);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<GalacticTrader.Data.Models.UserAccount?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Get user by username
    /// </summary>
    Task<GalacticTrader.Data.Models.UserAccount?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Get or create user from Keycloak token claims
    /// </summary>
    Task<GalacticTrader.Data.Models.UserAccount?> GetOrCreateFromTokenAsync(string keycloakId, string username, string email);

    /// <summary>
    /// Update user profile
    /// </summary>
    Task<GalacticTrader.Data.Models.UserAccount?> UpdateUserAsync(Guid userId, UpdateUserRequest request);

    /// <summary>
    /// Get user's roles
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);

    /// <summary>
    /// Assign role to user
    /// </summary>
    Task<bool> AssignRoleAsync(Guid userId, string role);

    /// <summary>
    /// Remove role from user
    /// </summary>
    Task<bool> RemoveRoleAsync(Guid userId, string role);

    /// <summary>
    /// Update last login timestamp
    /// </summary>
    Task<bool> UpdateLastLoginAsync(Guid userId);

    /// <summary>
    /// Get all users (admin only)
    /// </summary>
    Task<IEnumerable<GalacticTrader.Data.Models.UserAccount>> GetAllUsersAsync();

    /// <summary>
    /// Deactivate user account
    /// </summary>
    Task<bool> DeactivateUserAsync(Guid userId);
}
