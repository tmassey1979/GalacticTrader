using System.Security.Claims;
using GalacticTrader.Data;
using GalacticTrader.Services.Auth;
using GalacticTrader.Services.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GalacticTrader.API.Security;

public interface IEndpointAuthorizationService
{
    Task<IResult?> RequireMapAdminAsync(HttpContext context, CancellationToken cancellationToken);

    Task<IResult?> RequireAnyRoleAsync(
        HttpContext context,
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken);

    Task<IResult?> RequireOwnerOrAdminAsync(
        HttpContext context,
        Guid ownerPlayerId,
        CancellationToken cancellationToken);

    Task<(Guid? PlayerId, bool IsAdmin, IResult? Denied)> ResolveAuthenticatedActorAsync(
        HttpContext context,
        CancellationToken cancellationToken);

    Task<(Guid EffectivePlayerId, bool IsAdmin, IResult? Denied)> ResolveEffectivePlayerIdAsync(
        HttpContext context,
        Guid requestedPlayerId,
        CancellationToken cancellationToken);

    bool TryReadBearerToken(HttpContext context, out string token);
}

public sealed class EndpointAuthorizationService : IEndpointAuthorizationService
{
    private readonly IAuthService _authService;
    private readonly ITokenValidationService _tokenValidationService;
    private readonly GalacticTraderDbContext _dbContext;

    public EndpointAuthorizationService(
        IAuthService authService,
        ITokenValidationService tokenValidationService,
        GalacticTraderDbContext dbContext)
    {
        _authService = authService;
        _tokenValidationService = tokenValidationService;
        _dbContext = dbContext;
    }

    public Task<IResult?> RequireMapAdminAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return RequireAnyRoleAsync(
            context,
            [AuthorizationPolicies.AdminRole, AuthorizationPolicies.MapAdminRole],
            cancellationToken);
    }

    public async Task<IResult?> RequireAnyRoleAsync(
        HttpContext context,
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken)
    {
        if (!TryReadBearerToken(context, out var token))
        {
            return Results.Unauthorized();
        }

        var session = await _authService.ValidateTokenAsync(token, cancellationToken);
        if (session is not null)
        {
            var userAccount = await _dbContext.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    account =>
                        account.Username == session.Player.Username ||
                        account.Email == session.Player.Email,
                    cancellationToken);

            if (userAccount is null)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var hasAllowedRole = userAccount.Roles.Any(role =>
                allowedRoles.Any(allowed => role.Equals(allowed, StringComparison.OrdinalIgnoreCase)));

            return hasAllowedRole
                ? null
                : Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!LooksLikeJwt(token))
        {
            return Results.Unauthorized();
        }

        var principal = await _tokenValidationService.ValidateTokenAsync(token);
        if (principal is null)
        {
            return Results.Unauthorized();
        }

        var tokenRoles = _tokenValidationService.GetRoles(principal);
        var hasAllowedJwtRole = tokenRoles.Any(role =>
            allowedRoles.Any(allowed => role.Equals(allowed, StringComparison.OrdinalIgnoreCase)));

        return hasAllowedJwtRole
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    public async Task<IResult?> RequireOwnerOrAdminAsync(
        HttpContext context,
        Guid ownerPlayerId,
        CancellationToken cancellationToken)
    {
        var (playerId, isAdmin, denied) = await ResolveAuthenticatedActorAsync(context, cancellationToken);
        if (denied is not null)
        {
            return denied;
        }

        if (isAdmin)
        {
            return null;
        }

        if (!playerId.HasValue || playerId.Value != ownerPlayerId)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return null;
    }

    public async Task<(Guid? PlayerId, bool IsAdmin, IResult? Denied)> ResolveAuthenticatedActorAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!TryReadBearerToken(context, out var token))
        {
            return (null, false, Results.Unauthorized());
        }

        var session = await _authService.ValidateTokenAsync(token, cancellationToken);
        if (session is not null)
        {
            var userAccount = await _dbContext.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    account =>
                        account.Id == session.Player.PlayerId ||
                        account.Username == session.Player.Username ||
                        account.Email == session.Player.Email,
                    cancellationToken);

            if (userAccount is null)
            {
                return (session.Player.PlayerId, false, Results.StatusCode(StatusCodes.Status403Forbidden));
            }

            var isAdmin = userAccount.Roles.Any(role =>
                role.Equals(AuthorizationPolicies.AdminRole, StringComparison.OrdinalIgnoreCase));

            return (session.Player.PlayerId, isAdmin, null);
        }

        if (!LooksLikeJwt(token))
        {
            return (null, false, Results.Unauthorized());
        }

        var principal = await _tokenValidationService.ValidateTokenAsync(token);
        if (principal is null)
        {
            return (null, false, Results.Unauthorized());
        }

        var isJwtAdmin = _tokenValidationService.GetRoles(principal).Any(role =>
            role.Equals(AuthorizationPolicies.AdminRole, StringComparison.OrdinalIgnoreCase));

        var playerId = _tokenValidationService.GetUserId(principal)
            ?? await ResolvePlayerIdFromPrincipalAsync(principal, cancellationToken);

        return (playerId, isJwtAdmin, null);
    }

    public async Task<(Guid EffectivePlayerId, bool IsAdmin, IResult? Denied)> ResolveEffectivePlayerIdAsync(
        HttpContext context,
        Guid requestedPlayerId,
        CancellationToken cancellationToken)
    {
        var (callerPlayerId, isAdmin, denied) = await ResolveAuthenticatedActorAsync(context, cancellationToken);
        if (denied is not null)
        {
            return (Guid.Empty, isAdmin, denied);
        }

        if (!isAdmin)
        {
            if (!callerPlayerId.HasValue)
            {
                return (Guid.Empty, false, Results.StatusCode(StatusCodes.Status403Forbidden));
            }

            if (requestedPlayerId != Guid.Empty && requestedPlayerId != callerPlayerId.Value)
            {
                return (Guid.Empty, false, Results.StatusCode(StatusCodes.Status403Forbidden));
            }

            return (callerPlayerId.Value, false, null);
        }

        var effectivePlayerId = requestedPlayerId != Guid.Empty
            ? requestedPlayerId
            : callerPlayerId ?? Guid.Empty;

        if (effectivePlayerId == Guid.Empty)
        {
            return (Guid.Empty, true, Results.StatusCode(StatusCodes.Status403Forbidden));
        }

        return (effectivePlayerId, true, null);
    }

    public bool TryReadBearerToken(HttpContext context, out string token)
    {
        token = string.Empty;
        if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return false;
        }

        var headerValue = authorizationHeader.ToString();
        const string prefix = "Bearer ";
        if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extractedToken = headerValue[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(extractedToken))
        {
            return false;
        }

        token = extractedToken;
        return true;
    }

    private async Task<Guid?> ResolvePlayerIdFromPrincipalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var preferredUsername = principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;

        var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        var hasSubjectGuid = Guid.TryParse(subject, out var subjectGuid);

        var player = await _dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(
                existing =>
                    (!string.IsNullOrWhiteSpace(preferredUsername) && existing.Username == preferredUsername) ||
                    (!string.IsNullOrWhiteSpace(email) && existing.Email == email) ||
                    (hasSubjectGuid && existing.KeycloakUserId == subjectGuid),
                cancellationToken);

        return player?.Id;
    }

    private static bool LooksLikeJwt(string token)
    {
        return token.Count(character => character == '.') == 2;
    }
}
