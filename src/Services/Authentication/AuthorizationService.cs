namespace GalacticTrader.Services.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;

/// <summary>
/// Implementation of authorization service
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly GalacticTraderDbContext _dbContext;
    private readonly ITokenValidationService _tokenValidation;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        GalacticTraderDbContext dbContext,
        ITokenValidationService tokenValidation,
        ILogger<AuthorizationService> logger)
    {
        _dbContext = dbContext;
        _tokenValidation = tokenValidation;
        _logger = logger;
    }

    public async Task<AuthorizationContext?> GetAuthorizationContextAsync(Guid userId)
    {
        var user = await _dbContext.UserAccounts.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new AuthorizationContext
        {
            UserId = user.Id,
            Username = user.Username,
            Roles = user.Roles,
            TokenIssuedAt = DateTime.UtcNow,
            TokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }

    public async Task<AuthorizationContext?> CreateContextFromClaimsAsync(ClaimsPrincipal principal)
    {
        var userId = _tokenValidation.GetUserId(principal);
        if (userId == null)
        {
            _logger.LogWarning("Could not extract user ID from claims");
            return null;
        }

        // Get user and their roles from database
        var user = await _dbContext.UserAccounts.FindAsync(userId.Value);
        if (user == null)
        {
            _logger.LogWarning("User not found in database: {UserId}", userId);
            return null;
        }

        var roles = _tokenValidation.GetRoles(principal).ToList();
        
        // If user has roles in database but none in token, use database roles
        if (!roles.Any() && user.Roles.Any())
        {
            roles = user.Roles;
        }

        var issuedAtClaim = principal.FindFirst("iat");
        var expirationClaim = principal.FindFirst("exp");

        var context = new AuthorizationContext
        {
            UserId = user.Id,
            Username = user.Username,
            Roles = roles,
            CustomClaims = _tokenValidation.GetAllClaims(principal),
            TokenIssuedAt = issuedAtClaim != null && long.TryParse(issuedAtClaim.Value, out var iat)
                ? UnixTimeStampToDateTime(iat)
                : DateTime.UtcNow,
            TokenExpiresAt = expirationClaim != null && long.TryParse(expirationClaim.Value, out var exp)
                ? UnixTimeStampToDateTime(exp)
                : DateTime.UtcNow.AddHours(1)
        };

        return context;
    }

    public async Task<bool> CanAccessResourceAsync(Guid userId, string resourceType, string resourceId)
    {
        // Implement resource-level authorization checks
        // This is a simplified example - in a real app, you'd check resource ownership
        
        var user = await _dbContext.UserAccounts.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Admins can access everything
        if (user.Roles.Contains(AuthorizationPolicies.AdminRole))
        {
            return true;
        }

        // Implement specific resource checks here
        switch (resourceType.ToLower())
        {
            case "ship":
                return await CanAccessShipAsync(userId, resourceId);
            case "fleet":
                return await CanAccessFleetAsync(userId, resourceId);
            case "market":
                return await CanAccessMarketAsync(userId, resourceId);
            default:
                _logger.LogWarning("Unknown resource type: {ResourceType}", resourceType);
                return false;
        }
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var user = await _dbContext.UserAccounts.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Admin and Moderator have all permissions
        if (user.Roles.Contains(AuthorizationPolicies.AdminRole) ||
            user.Roles.Contains(AuthorizationPolicies.ModeratorRole))
        {
            return true;
        }

        // Implement permission mapping here
        // This is where you'd check permissions based on roles
        return permission switch
        {
            "trade" => user.Roles.Contains(AuthorizationPolicies.PlayerRole),
            "combat" => user.Roles.Contains(AuthorizationPolicies.PlayerRole),
            "navigate" => user.Roles.Contains(AuthorizationPolicies.PlayerRole),
            "manage_players" => user.Roles.Contains(AuthorizationPolicies.ModeratorRole),
            "manage_economy" => user.Roles.Contains(AuthorizationPolicies.AdminRole),
            _ => false
        };
    }

    public bool ValidateContext(AuthorizationContext context)
    {
        // Check if context is valid and token hasn't expired
        return context.IsTokenValid && 
               !string.IsNullOrEmpty(context.Username) && 
               context.UserId != Guid.Empty;
    }

    // Private resource access check methods
    private async Task<bool> CanAccessShipAsync(Guid userId, string shipId)
    {
        if (!Guid.TryParse(shipId, out var shipGuid))
            return false;

        var ship = await _dbContext.Ships
            .FirstOrDefaultAsync(s => s.Id == shipGuid && s.PlayerId == userId);

        return ship != null;
    }

    private async Task<bool> CanAccessFleetAsync(Guid userId, string fleetId)
    {
        // Implement fleet access check
        // Would typically check if user owns the fleet
        return await Task.FromResult(true);
    }

    private async Task<bool> CanAccessMarketAsync(Guid userId, string marketId)
    {
        // Markets are typically public, but some might be restricted
        return await Task.FromResult(true);
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}
