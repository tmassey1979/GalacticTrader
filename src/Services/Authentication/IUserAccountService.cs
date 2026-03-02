namespace GalacticTrader.Services.Authentication;

using System.ComponentModel.DataAnnotations;
using GalacticTrader.Data.Models;

/// <summary>
/// Represents a user account with authentication details
/// </summary>
public class UserAccount
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string KeycloakId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request to create a new user account
/// </summary>
public class CreateUserRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request to update user profile
/// </summary>
public class UpdateUserRequest
{
    [MinLength(3)]
    public string? FirstName { get; set; }

    [MinLength(3)]
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

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
