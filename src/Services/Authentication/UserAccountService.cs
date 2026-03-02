namespace GalacticTrader.Services.Authentication;

using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;

/// <summary>
/// Implementation of user account service with database persistence
/// </summary>
public class UserAccountService : IUserAccountService
{
    private readonly GalacticTraderDbContext _dbContext;
    private readonly ILogger<UserAccountService> _logger;

    public UserAccountService(
        GalacticTraderDbContext dbContext,
        ILogger<UserAccountService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Data.Models.UserAccount> CreateUserAsync(CreateUserRequest request)
    {
        // Check if user already exists
        var existingUser = await GetUserByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken");
        }

        var user = new Data.Models.UserAccount
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.UserAccounts.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User account created: {UserId} ({Username})", user.Id, user.Username);

        return user;
    }

    public async Task<Data.Models.UserAccount?> GetUserByIdAsync(Guid userId)
    {
        return await _dbContext.UserAccounts.FindAsync(userId);
    }

    public async Task<Data.Models.UserAccount?> GetUserByUsernameAsync(string username)
    {
        return await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<Data.Models.UserAccount?> GetOrCreateFromTokenAsync(string keycloakId, string username, string email)
    {
        // Try to find existing user by Keycloak ID
        var user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

        if (user != null)
        {
            return user;
        }

        // Try to find by username
        user = await GetUserByUsernameAsync(username);

        if (user == null)
        {
            // Create new user
            user = new Data.Models.UserAccount
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                KeycloakId = keycloakId,
                FirstName = username,
                LastName = "Player",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.UserAccounts.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("New user created from token: {UserId} ({Username})", user.Id, user.Username);

            return user;
        }

        // Update existing user with Keycloak ID
        user.KeycloakId = keycloakId;
        _dbContext.UserAccounts.Update(user);
        await _dbContext.SaveChangesAsync();

        return user;
    }

    public async Task<Data.Models.UserAccount?> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrEmpty(request.LastName))
        {
            user.LastName = request.LastName;
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            user.Email = request.Email;
        }

        _dbContext.UserAccounts.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User profile updated: {UserId}", userId);

        return user;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        return user?.Roles ?? [];
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string role)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return true; // Already has role
        }

        user.Roles.Add(role);
        _dbContext.UserAccounts.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Role assigned to user: {UserId} -> {Role}", userId, role);

        return true;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string role)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var removed = user.Roles.RemoveAll(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
        {
            _dbContext.UserAccounts.Update(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Role removed from user: {UserId} -> {Role}", userId, role);
        }

        return removed;
    }

    public async Task<bool> UpdateLastLoginAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.LastLoginAt = DateTime.UtcNow;
        _dbContext.UserAccounts.Update(user);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Data.Models.UserAccount>> GetAllUsersAsync()
    {
        return await _dbContext.UserAccounts.ToListAsync();
    }

    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        _dbContext.UserAccounts.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User deactivated: {UserId}", userId);

        return true;
    }
}
